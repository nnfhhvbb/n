﻿using System;
using System.IO;
using CommonTestUtils;
using GitUI.CommandsDialogs;
using NUnit.Framework;

namespace GitUITests.CommandsDialogs
{
    [TestFixture]
    public sealed class FormFileHistoryControllerTests
    {
        private FormFileHistoryController _controller;

        [SetUp]
        public void Setup()
        {
            _controller = new FormFileHistoryController();
        }

        [TestCase(@"Does not exist")]
        [TestCase("")]
        [TestCase(" ")]
        public void TryGetExactPathName_Should_return_null_on_not_existing_file(string path)
        {
            var lowercasePath = path.ToLower();
            var isExistingOnFileSystem = _controller.TryGetExactPath(lowercasePath, out string exactPath);

            Assert.IsFalse(isExistingOnFileSystem);
            Assert.IsNull(exactPath);
        }

        [TestCase(@"c:\Users\Public\desktop.ini")]
        [TestCase(@"c:\pagefile.sys")]
        [TestCase(@"c:\Windows\System32\cmd.exe")]
        [TestCase(@"c:\Users\Default\NTUSER.DAT")]
        [TestCase(@"c:\Program Files (x86)\Microsoft.NET\Primary Interop Assemblies")]
        [TestCase(@"c:\Program Files (x86)")]
        public void TryGetExactPathName_Should_output_path_with_exact_casing(string path)
        {
            var lowercasePath = path.ToLower();
            var isExistingOnFileSystem = _controller.TryGetExactPath(lowercasePath, out string exactPath);

            Assert.IsTrue(isExistingOnFileSystem);
            Assert.AreEqual(path, exactPath);
        }

        [Test]
        public void TryGetExactPathName_Should_handle_network_path()
        {
            var path = @"\\" + Environment.MachineName.ToLower() + @"\c$\Windows\System32";

            var lowercasePath = path.ToLower();
            var isExistingOnFileSystem = _controller.TryGetExactPath(lowercasePath, out string exactPath);

            Assert.IsTrue(isExistingOnFileSystem);

            Assert.AreEqual(path, exactPath);
        }

        [TestCase("Folder1\\file1.txt", true, true)]
        [TestCase("FOLDER1\\file1.txt", true, false)]
        [TestCase("fOLDER1\\file1.txt", true, false)]
        [TestCase("Folder2\\file1.txt", false, false)]
        public void TryGetExactPathName_should_check_if_path_matches_case(string relativePath, bool isResolved, bool doesMatch)
        {
            using (var repo = new GitModuleTestHelper())
            {
                // Create a file
                var notUsed = repo.CreateFile(Path.Combine(repo.TemporaryPath, "Folder1"), "file1.txt", "bla");

                var expected = Path.Combine(repo.TemporaryPath, relativePath);

                Assert.AreEqual(isResolved, _controller.TryGetExactPath(expected, out string exactPath));
                Assert.AreEqual(doesMatch, exactPath == expected);
            }
        }
    }
}