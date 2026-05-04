using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class SynoNasTreeNodeDM
    {
        public class SynologyOptions
        {
            public string BaseUrl { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        public class NasTreeNodeDto
        {
            public string Id { get; set; } = string.Empty;       // 建議直接用 path
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public bool IsDir { get; set; }
            public bool HasChildren { get; set; } = true;
            public bool Loaded { get; set; } = false;
            public bool Expanded { get; set; } = false;
            public List<NasTreeNodeDto> Children { get; set; } = new();
        }

        public class SynologyError
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }
        }

        public class SynologyLoginResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("data")]
            public SynologyLoginData? Data { get; set; }

            [JsonPropertyName("error")]
            public SynologyError? Error { get; set; }
        }

        public class SynologyLoginData
        {
            [JsonPropertyName("sid")]
            public string Sid { get; set; } = string.Empty;
        }

        public class SynologyListShareResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("data")]
            public SynologyListShareData? Data { get; set; }

            [JsonPropertyName("error")]
            public SynologyError? Error { get; set; }
        }

        public class SynologyListShareData
        {
            [JsonPropertyName("offset")]
            public int Offset { get; set; }

            [JsonPropertyName("total")]
            public int Total { get; set; }

            [JsonPropertyName("shares")]
            public List<SynologyFileItem> Shares { get; set; } = new();
        }

        public class SynologyListResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("data")]
            public SynologyListData? Data { get; set; }

            [JsonPropertyName("error")]
            public SynologyError? Error { get; set; }
        }

        public class SynologyListData
        {
            [JsonPropertyName("offset")]
            public int Offset { get; set; }

            [JsonPropertyName("total")]
            public int Total { get; set; }

            [JsonPropertyName("files")]
            public List<SynologyFileItem> Files { get; set; } = new();
        }

        public class SynologyFileItem
        {
            [JsonPropertyName("path")]
            public string Path { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("isdir")]
            public bool IsDir { get; set; }

            [JsonPropertyName("children")]
            public SynologyChildrenData? Children { get; set; }

            [JsonPropertyName("additional")]
            public SynologyFileAdditional? Additional { get; set; }
        }

        public class SynologyChildrenData
        {
            [JsonPropertyName("offset")]
            public int Offset { get; set; }

            [JsonPropertyName("total")]
            public int Total { get; set; }

            [JsonPropertyName("files")]
            public List<SynologyFileItem> Files { get; set; } = new();
        }

        public class SynologyFileAdditional
        {
            [JsonPropertyName("real_path")]
            public string? RealPath { get; set; }
        }
    }
}
