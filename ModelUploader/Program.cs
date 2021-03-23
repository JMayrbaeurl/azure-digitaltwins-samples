using System;

using Azure.DigitalTwins.Core;
using Azure.Identity;

using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Azure;
using CommandLine;

using System.Linq;

namespace smartcity_helper
{

    class Program
    {
        private static DigitalTwinsClient client;
        private static string modelPath;
        private static bool deleteFirst;
        private static string adtInstanceUrl;

         private class CliOptions
        {
            [Option('p', "path", Required = true, HelpText = "The path to the on-disk directory holding DTDL models.")]
            public string ModelPath { get; set; }
            [Option('d', "deletefirst", Required = false, HelpText = "Specify if you want to delete the models first, by default is false")]
            public bool DeleteFirst { get; set; }
            [Option('i', "adtinsturl", Required = true, HelpText = "The ADT instance URL.")]
            public string AdtInstanceUrl { get; set; }
        }
        
        static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<CliOptions>(args)
                   .WithParsed(o =>
                   {
                       modelPath = o.ModelPath;
                       deleteFirst = o.DeleteFirst;
                       adtInstanceUrl = o.AdtInstanceUrl;
                   }
                   );

            var credential = new DefaultAzureCredential();
            client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
            Console.WriteLine($"ADT client created – ready to go");

            if (deleteFirst) {
                Console.WriteLine();
                Console.WriteLine($"Deleting all existing ontology models");

                DeleteAllModels(1);
            }

            Console.WriteLine();
            Console.WriteLine($"Uploading all ontology models");
            
            string[] modelPaths = Directory.GetFiles(modelPath, "*.json", SearchOption.AllDirectories);
            var models = new List<string>();
            foreach (var model in modelPaths)
            {
                string dtdl = File.ReadAllText(model);
                models.Add(dtdl);
            }
            // Upload the models to the service
            await client.CreateModelsAsync(models);

            Console.WriteLine($"Created " + models.Count + " models in " + adtInstanceUrl);
        }

        private static void DeleteAllModels(int iteration)
        {
            foreach (DigitalTwinsModelData md in client.GetModels())
            {
                try
                {
                    client.DeleteModel(md.Id);
                    Console.WriteLine("Successfully deleted Model {" + md.Id + "}. Attempt [" + iteration + "]");
                }
                catch (RequestFailedException)
                {
                    //Log.Error("Failed to delete Model {" + md.Id + "}");
                    //Log.Error(e2.Message);
                    // skip this and go to the next one
                }
            }

            try
            {
                IEnumerable<DigitalTwinsModelData> c = client.GetModels() as IEnumerable<DigitalTwinsModelData>;
                if (c.Count<DigitalTwinsModelData>() > 0) DeleteAllModels(iteration + 1);
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
