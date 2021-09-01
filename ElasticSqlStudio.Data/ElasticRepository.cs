using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;

namespace ElasticSqlStudio.Data
{
    public class ElasticRepository
    {
        private readonly ElasticClient client;

        public ElasticRepository(string url, string user, string password)
        {
            var uri = new Uri(url);
            var connection = new ConnectionSettings(uri);
            connection.BasicAuthentication(user, password);

            // tracing elastic, needs more performance so only comment in when needed
            //connection.DisableDirectStreaming().OnRequestCompleted(details =>
            //    {
            //        if (details.RequestBodyInBytes != null)
            //        {
            //            Trace.WriteLine($"### ES REQEUST ###{Environment.NewLine}{Encoding.UTF8.GetString(details.RequestBodyInBytes)}, Uri: {details.Uri} Method: {details.HttpMethod}");
            //        }
            //        if (details.ResponseBodyInBytes != null)
            //        {
            //            Trace.WriteLine($"### ES RESPONSE ###{Environment.NewLine}{Encoding.UTF8.GetString(details.ResponseBodyInBytes)}");
            //        }
            //    }).PrettyJson();

            this.client = new ElasticClient(connection); }

        public async Task PingElastic()
        {
            var response = await this.client.PingAsync();
            if (!response.IsValid)
            {
                throw response.OriginalException;
            }
        }

        public async Task<string> RunSingleQuery(string query)
        {
            if (query.Contains("LIMIT"))
            {
                throw new ArgumentException("Do not use LIMIT, thx ;)");
            }

            var request = new QuerySqlRequest { Query = query + " LIMIT 1", Format = "json" };
            var response = await this.client.Sql.QueryAsync(request);

            if (response.IsValid)
            {
                return this.ConvertRows2CSV(response.Rows);
            }

            throw response.OriginalException;
        }

        public async Task<string> QueryComplete(string query)
        {
            var request = new QuerySqlRequest
            {
                Query = query, 
                Format = "json", FetchSize = 1000
            };

            var response = await this.client.Sql.QueryAsync(request);

            if (response.IsValid)
            {
                var rtnString = this.ConvertRows2CSV(response.Rows);
                var cursor = response.Cursor;
                while (!string.IsNullOrEmpty(cursor))
                {
                    var cursorResponse = await this.client.Sql.QueryAsync(new QuerySqlRequest { Cursor = response.Cursor });
                    rtnString += this.ConvertRows2CSV(cursorResponse.Rows);
                    cursor = cursorResponse.Cursor;
                }

                return rtnString;
            }
            
            throw response.OriginalException;
        }

        private string ConvertRows2CSV(IReadOnlyCollection<SqlRow> responseRows)
        {
            var rtnString = string.Empty;
            foreach (var responseRow in responseRows)
            {
                foreach (var sqlValue in responseRow)
                {
                    try
                    {
                        rtnString += $"{sqlValue?.As<string>()};";
                    }
                    catch (Exception)
                    {
                        rtnString += "CAN'T_PARSE_THIS_AS_STRING;";
                    }
                }
                rtnString += Environment.NewLine;
            }

            return rtnString;
        }
    }
}
