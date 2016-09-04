using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CRFSharp.decoder
{
    /// <summary>
    /// Provides read-only access to a model stored in the file system
    /// in three parts - metadata, feature vectors and feature weights.
    /// </summary>
    public sealed class DefaultModelReader : ModelReaderBase

    { 
        /// <summary>
        /// Returns the full path to model file.
        /// </summary>
        public string FilePath { get; private set; }


        /// <summary>
        /// Initializes a new instance of the model reader
        /// using the specified <paramref name="filePath"/>
        /// </summary>
        /// <param name="filePath"></param>
        public DefaultModelReader(string filePath)
        {
            filePath.ThrowIfNotExists();
            FilePath = filePath;
        }

        /// <summary>
        /// Returns the model metadata stream.
        /// </summary>
        /// <returns></returns>
        protected override Stream GetMetadataStream()
        {
            string path = FilePath.ToMetadataModelName();
            return File.OpenRead(path);
        }

        /// <summary>
        /// Returns the model feature set stream.
        /// </summary>
        /// <returns></returns>
        protected override Stream GetFeatureSetStream()
        {
            string path = FilePath.ToFeatureSetFileName();

            return File.OpenRead(path);
        }

        /// <summary>
        /// Returns the model feature weight stream.
        /// </summary>
        /// <returns></returns>
        protected override Stream GetFeatureWeightStream()
        {
            string path = FilePath.ToFeatureWeightFileName();
            return File.OpenRead(path);
        }

    }
}
