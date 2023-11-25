using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongodbTestLib.Entities
{
    public record GetProjectResponse
    {
        public string IId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CvssEnvironmentalVector { get; set; }
        public List<SbomResultResponse> SbomResults { get; set; }
    }

    public record SearchResponse
    {
        public List<SearchResponseItem> Results { get; set; } = new();
    }

    public record SearchResponseItem
    {
        public string Component { get; set; }
        public List<SearchProjectResponse> Projects { get; set; } = new();
    }

    public record SearchProjectResponse
    {
        public string IId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<SbomResultResponse> SbomResults { get; set; } = new();
    }

    public record SbomResultResponse
    {
        public string IId { get; set; }
        public string Version { get; set; }
    }
}
