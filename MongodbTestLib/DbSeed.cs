using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongodbTestLib.Entities;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tynamix.ObjectFiller;

namespace MongodbTestLib
{
    public static class DbSeed
    {
        public static IMongoCollection<Project> _projects;
        private static IMongoCollection<SbomResult> _sbomResults;
        private static IMongoCollection<BaseVulnerability> _baseVulnerabilities;
        private static IMongoCollection<Component> _components;
        private static IMongoCollection<Composition> _compositions;

        public static void Seed(int count = 10)
        {
            var client = new MongoClient("mongodb://user:pass@localhost:27017");
            var database = client.GetDatabase("Test");
            _projects = database.GetCollection<Project>("Projects");
            _sbomResults = database.GetCollection<SbomResult>("SbomResults");
            _baseVulnerabilities = database.GetCollection<BaseVulnerability>("BaseVulnerabilities");
            _components = database.GetCollection<Component>("Components");
            _compositions = database.GetCollection<Composition>("Compositions");

            // clean up
            _projects.DeleteMany(x => true);
            _sbomResults.DeleteMany(x => true);
            _baseVulnerabilities.DeleteMany(x => true);
            _components.DeleteMany(x => true);
            _compositions.DeleteMany(x => true);

            // setup filler
            var projectFiller = new Filler<Project>();
            projectFiller.Setup().OnProperty(x => x.Id).IgnoreIt().OnProperty(x => x.SbomResultIds).Use(() => Enumerable.Range(1, new Random().Next(1,20)).Select(x => ObjectId.GenerateNewId()).ToList());

            var sbFiller = new Filler<SbomResult>();
            sbFiller.Setup().OnProperty(x => x.Id).IgnoreIt().OnProperty(x => x.Vulnerabilities).IgnoreIt().OnProperty(x=> x.ProjectId).IgnoreIt();

            var vulFiller = new Filler<Vulnerability>();
            vulFiller.Setup().OnProperty(x => x.Id).Use(() => Guid.NewGuid().ToString()).OnProperty(x => x.BaseVulnerabilityId).IgnoreIt().OnProperty(x => x.ComponentId).IgnoreIt();

            var bVulFiller = new Filler<BaseVulnerability>();
            bVulFiller.Setup().OnProperty(x => x.Id).IgnoreIt();

            var compFiller = new Filler<Component>();
            compFiller.Setup().OnProperty(x => x.Id).IgnoreIt();

            // seed db
            var projects = projectFiller.Create(count);
            foreach (var project in projects)
            {
                _projects.InsertOne(project);
                var sbomResults = sbFiller.Create(project.SbomResultIds.Count);
                var i = 0;
                foreach(var sbomResult in sbomResults)
                {
                    var vuls = vulFiller.Create(new Random().Next(1, count*3));
                    foreach(var vul in vuls)
                    {
                        var comp = compFiller.Create();
                        _components.InsertOne(comp);
                        var bVul = bVulFiller.Create();
                        _baseVulnerabilities.InsertOne(bVul);

                        vul.ComponentId = comp.Id;
                        vul.BaseVulnerabilityId = bVul.Id;
                    }
                    sbomResult.ProjectId = project.Id;
                    sbomResult.Vulnerabilities = vuls.ToList();
                    _sbomResults.InsertOne(sbomResult);
                    foreach (var vul in vuls)
                    {
                        _compositions.InsertOne(new Composition()
                        {
                            ComponentId = vul.ComponentId,
                            SbomResultId = sbomResult.Id
                        });
                    }

                    project.SbomResultIds[i] = sbomResult.Id;
                    _projects.ReplaceOne(x => x.Id == project.Id, project);
                    i++;
                }

            }
            var compIds =  _components.AsQueryable().Sample(count*2).Select(x => x.Id).ToList();
            var sbomREsultIds =  _components.AsQueryable().Sample(count*2).Select(x => x.Id).ToList();

            for (int i = 0; i < Math.Min(compIds.Count, sbomREsultIds.Count); i++)
            {
                _compositions.InsertOne(new Composition()
                {
                    ComponentId = compIds[i],
                    SbomResultId = sbomREsultIds[i]
                }) ;
            }
        }
    }
}
