using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.IO;

namespace GdalBindingNetCore
{
    class Program
    {
        protected static IMongoClient _client;
        protected static IMongoDatabase _database;

        /// <summary>
        /// Import shp to mongodb
        /// </summary>
        public static void ShpToMongo()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _database = _client.GetDatabase("geodb");
            var _collection = _database.GetCollection<BsonDocument>("tinh");
            // create geo index
            var index = Builders<BsonDocument>.IndexKeys.Geo2DSphere("geometry");
            _collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(index));


            // a common process is : datasource (ds) -> layer -> feature -> geometry

            Gdal.AllRegister();
            Ogr.RegisterAll();

            string shapeFilePath = @"C:\Users\Admin\Downloads\vungkt\tinh.shp";

            var drv = Ogr.GetDriverByName("ESRI Shapefile");

            var ds = drv.Open(shapeFilePath, 0);

            OSGeo.OGR.Layer layer = ds.GetLayerByIndex(0);
            OSGeo.OGR.Feature f;
            layer.ResetReading();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            var listDocument = new List<BsonDocument>();
            //sb.AppendLine("{\"type\":\"FeatureCollection\", \"features\":[");

            while ((f = layer.GetNextFeature()) != null)
            {
                sb.Clear();
                //Geometry
                var geom = f.GetGeometryRef();
                if (geom != null)
                {
                    var geometryJson = geom.ExportToJson(new string[] { });
                    sb.Append("{\"type\":\"Feature\",\"geometry\":" + geometryJson + ",");
                }

                //Properties
                int count = f.GetFieldCount();
                if (count != 0)
                {
                    sb.Append("\"properties\":{");
                    for (int i = 0; i < count; i++)
                    {
                        FieldType type = f.GetFieldType(i);
                        string key = f.GetFieldDefnRef(i).GetName();

                        if (type == FieldType.OFTInteger)
                        {
                            var field = f.GetFieldAsInteger(i);
                            sb.Append("\"" + key + "\":" + field + ",");
                        }
                        else if (type == FieldType.OFTReal)
                        {
                            var field = f.GetFieldAsDouble(i);
                            sb.Append("\"" + key + "\":" + field + ",");
                        }
                        else
                        {
                            var field = f.GetFieldAsString(i);
                            sb.Append("\"" + key + "\":\"" + field + "\",");
                        }

                    }
                    sb.Length--;
                    sb.Append("},");
                }

                //FID
                long id = f.GetFID();
                sb.AppendLine("\"id\":" + id + "}\n");
                var document = BsonSerializer.Deserialize<BsonDocument>(sb.ToString());
                listDocument.Add(document);

            }
            _collection.InsertMany(listDocument);

            //sb.Length -= 3;
            //sb.AppendLine("");
            ////sb.Append("]}");

            //File.WriteAllText(@"C:\Users\Admin\Downloads\vungkt\tinh.geojson", sb.ToString());
        }

        static void Main(string[] args)
        {
            ShpToMongo();
            // connect mongo
            //_client = new MongoClient("mongodb://localhost:27017");
            //_database = _client.GetDatabase("geodb");
            //var _collection = _database.GetCollection<BsonDocument>("xa1");

            //var point = GeoJson.Polygon(GeoJson.Geographic(103, 22), GeoJson.Geographic(105, 20), GeoJson.Geographic(106, 21), GeoJson.Geographic(103, 22));

            //var locationQuery = new FilterDefinitionBuilder<BsonDocument>().GeoWithin("geometry", point);

            //var nearData = _collection.Find(locationQuery);

            //var data = nearData.ToList();
        }
    }
}
