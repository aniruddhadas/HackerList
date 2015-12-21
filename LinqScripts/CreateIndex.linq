<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference Prerelease="true">Microsoft.Azure.Search</NuGetReference>
  <Namespace>Microsoft.Azure.Search</Namespace>
  <Namespace>Microsoft.Azure.Search.Models</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

private static SearchServiceClient _searchClient;
private static SearchIndexClient _indexClient;

private static string SearchServiceName = "<servicename here>";
private static string SearchServiceApiKey = "<key here>";

void Main()
{
 	string searchServiceName = SearchServiceName;
	string apiKey = SearchServiceApiKey;
	
	// Create an HTTP reference to the catalog index
	_searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
	_indexClient = _searchClient.Indexes.GetClient("<YOUR INDEX NAME>");
	
//	Console.WriteLine("{0}", "Upload document...\n");
//	DataIndexer.UploadTestDocument();
	
//	return;
	
	Console.WriteLine("{0}", "Deleting index...\n");
	if (DataIndexer.DeleteIndex())
	{
		Console.WriteLine("{0}", "Creating index...\n");
		DataIndexer.CreateIndex();
		Console.WriteLine("{0}", "Upload document...\n");
		DataIndexer.UploadTestDocument();
//		DataIndexer.SyncDataFromAzureSQL();
	}
}


public static class DataIndexer
{
	public static bool DeleteIndex()
    {
		// Delete the index if it exists
		try
		{
			_searchClient.Indexes.Delete("<YOUR INDEX NAME>");
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error deleting index: {0}\r\n", ex.Message.ToString());
			Console.WriteLine("Did you remember to add your SearchServiceName and SearchServiceApiKey to the app.config?\r\n");
			return false;
		}
	
		return true;
    }

   public static void CreateIndex()
   {
       // Create the Azure Search index based on the included schema
       try
       {
           var definition = new Index()
           {
               Name = "<YOUR INDEX NAME>",
               Fields = new[]
               {
                   new Field("URL_ID",     DataType.String)         { IsKey = true,  IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                   new Field("URL_FULL",     DataType.String)         { IsKey = false,  IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = false, IsRetrievable = true},
                   new Field("DOMAIN",     DataType.String)         { IsKey = false,  IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = true, IsRetrievable = true},
                   new Field("TEXT",    DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = false,  IsSortable = false,  IsFacetable = false, IsRetrievable = true},
                   new Field("TITLE",    DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = false,  IsSortable = false,  IsFacetable = false, IsRetrievable = true},
                   new Field("CONTENT_TYPE",    DataType.String)     { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                   new Field("OWNER_ID",    DataType.String)     { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                   new Field("DATE_CREATED",   DataType.DateTimeOffset) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true},
                   new Field("DATE_EDITED",    DataType.DateTimeOffset) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true}
               }
           };

           _searchClient.Indexes.Create(definition);
       }
       catch (Exception ex)
       {
           Console.WriteLine("Error creating index: {0}\r\n", ex.Message.ToString());
       }

   }

	public static void UploadTestDocument() 
	{
		// This will use the Azure Search Indexer to synchronize data from Azure SQL to Azure Search
        Uri _serviceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
        HttpClient _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("api-key", SearchServiceApiKey);	
		
		Console.WriteLine("{0}", "Uploading document...\n");
		Uri uri = new Uri(_serviceUri, "/indexes/<YOUR INDEX NAME>/docs/index");
		
		string urlId = System.Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("http://www.test.com/"));
		
	    string document = @"
{{
  ""value"": [
    {{
      ""@search.action"": ""upload"",
      ""URL_ID"": ""{0}"",
      ""URL_FULL"": ""http://www.test.com/"",
      ""DOMAIN"": ""www.test.com"",
      ""TEXT"": ""bunch of intresting content"",
      ""TITLE"": ""test content"",
      ""CONTENT_TYPE"": ""HTML"", 
      ""OWNER_ID"": ""custom:bobdob2"",
      ""DATE_CREATED"": ""2015-09-07T00:00:00Z"",
      ""DATE_EDITED"": ""2015-09-07T00:00:00Z""
    }}
  ]
}}";

		document = string.Format(document, urlId);

		HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Post, uri, document);
		if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
		{
			Console.WriteLine("Error uploading document: {0}", response.Content.ReadAsStringAsync().Result);
			return;
		}		
	}
}

// Define other methods and classes here
public class AzureSearchHelper
{
   public const string ApiVersionString = "api-version=2015-02-28";

   private static readonly JsonSerializerSettings _jsonSettings;

   static AzureSearchHelper()
   {
       _jsonSettings = new JsonSerializerSettings
       {
           Formatting = Newtonsoft.Json.Formatting.Indented, // for readability, change to None for compactness
           ContractResolver = new CamelCasePropertyNamesContractResolver(),
           DateTimeZoneHandling = DateTimeZoneHandling.Utc
       };

       _jsonSettings.Converters.Add(new StringEnumConverter());
   }

   public static string SerializeJson(object value)
   {
       return JsonConvert.SerializeObject(value, _jsonSettings);
   }

   public static T DeserializeJson<T>(string json)
   {
       return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
   }

   public static HttpResponseMessage SendSearchRequest(HttpClient client, HttpMethod method, Uri uri, string json = null)
   { 
       UriBuilder builder = new UriBuilder(uri);
       string separator = string.IsNullOrWhiteSpace(builder.Query) ? string.Empty : "&";
       builder.Query = builder.Query.TrimStart('?') + separator + ApiVersionString;

       var request = new HttpRequestMessage(method, builder.Uri);

       if (json != null)
       {
           request.Content = new StringContent(json, Encoding.UTF8, "application/json");
       }

       return client.SendAsync(request).Result;
   }

   public static void EnsureSuccessfulSearchResponse(HttpResponseMessage response)
   {
       if (!response.IsSuccessStatusCode)
       {
           string error = response.Content == null ? null : response.Content.ReadAsStringAsync().Result;
           throw new Exception("Search request failed: " + error);
       }
   }
}