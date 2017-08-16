﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SandyBox.HostingService.Interop
{
    public abstract class Sandbox : IDisposable
    {
        protected Sandbox(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public abstract Task LoadFromAsync(Stream sourceStream, string sourceName);
        
        public virtual async Task LoadFromAsync(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                await LoadFromAsync(fs, Path.GetFileName(fileName));
            }
        }

        public abstract Task<JToken> ExecuteAsync(string functionName, JArray positionalParameters, JObject namedParameters);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Sandbox()
        {
            Dispose(false);
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }

}
