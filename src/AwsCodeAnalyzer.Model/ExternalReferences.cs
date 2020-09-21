﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AwsCodeAnalyzer.Model
{
    public class ExternalReferences
    {
        public ExternalReferences()
        {
            NugetReferences = new List<ExternalReference>();
            SdkReferences = new List<ExternalReference>();
            ProjectReferences = new List<ExternalReference>();
        }
        [JsonProperty("nuget", Order = 1)]
        public List<ExternalReference> NugetReferences { get; set; }
        [JsonProperty("sdk", Order = 2)]
        public List<ExternalReference> SdkReferences { get; set; }
        [JsonProperty("project", Order = 3)]
        public List<ExternalReference> ProjectReferences { get; set; }
    }
}