using System;
using System.IO;
using CRFSharp;
using CRFSharp.decoder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CRFSharpTests
{
    [TestClass]
    public class ModelReaderTests
    {
        private static readonly string _testModelPath = @"TestData\samplemodel";

        [TestMethod]
        public void When_GivenValidModelPath_Then_ShouldLoadModel()
        {
            // Arrange
            var testTarget = new ModelReader(_testModelPath);
            int expectedFeatureSize = 470517; // figured out based on model metadata

            // Act
            testTarget.LoadModel();

            // Assert
            Assert.AreEqual(testTarget.feature_size(), expectedFeatureSize);
        }

        [TestMethod]
        public void When_GivenValidModelLoader_Then_ShouldLoadModel()
        {
            // Arrange
            Func<string, Stream> fakeLoader = modelName =>
                new MemoryStream(File.ReadAllBytes(modelName));
            var testTarget = new ModelReader(fakeLoader, _testModelPath);
            int expectedFeatureSize = 470517; // figured out based on model metadata

            // Act
            testTarget.LoadModel();

            // Assert
            Assert.AreEqual(testTarget.feature_size(), expectedFeatureSize);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void When_GivenNonExistentModelPath_Then_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var testTarget = new ModelReader(
                _testModelPath + "some nonexistant garbage");

            // Act
            testTarget.LoadModel();
        }

    }
}
