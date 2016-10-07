using System;
using System.IO;
using CRFSharp.decoder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CRFSharpTests
{
    [TestClass]
    public class BareFilesModelReaderTests
    {
        private static readonly string _testModelPath = @"TestData\samplemodel";
        [TestMethod]
        public void When_GivenValidModelPath_Then_ShouldLoadItSuccessfully()
        {
            // Arrange
            var testTarget = new DefaultModelReader(_testModelPath);

            // Act
            testTarget.LoadModel();
            
            //Stream metadataStream = testTarget.GetMetadataStream();
            //Stream featureSetStream = testTarget.GetFeatureSetStream();
            //Stream featureWeightStream = testTarget.GetFeatureWeightStream();

            // Assert
            //Assert.AreEqual(metadataStream.Length, 401);
            //Assert.AreEqual(featureSetStream.Length, 93384);
            //Assert.AreEqual(featureWeightStream.Length, 5296);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void When_GivenNonExistentModelPath_Then_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var testTarget = new DefaultModelReader(
                _testModelPath + "some nonexistant garbage");

            // Act
            testTarget.LoadModel();
        }

    }
}
