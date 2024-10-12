﻿using System.Diagnostics;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using ResourceManager;

namespace GitUI.CommandsDialogs
{
    public partial class FormMailMap : GitModuleForm
    {
        private readonly TranslationString _mailmapOnlyInWorkingDirSupported =
            new(".mailmap is only supported when there is a working directory.");
        private readonly TranslationString _mailmapOnlyInWorkingDirSupportedCaption =
            new("No working directory");

        private readonly TranslationString _cannotAccessMailmap =
            new("Failed to save .mailmap." + Environment.NewLine + "Check if file is accessible.");
        private readonly TranslationString _cannotAccessMailmapCaption =
            new("Failed to save .mailmap");

        private readonly TranslationString _saveFileQuestion =
            new("Save changes to .mailmap?");
        private readonly TranslationString _saveFileQuestionCaption =
            new("Save changes?");

        public string MailMapFile = string.Empty;
        private readonly IFullPathResolver _fullPathResolver;

        public FormMailMap(IGitUICommands commands)
            : base(commands)
        {
            InitializeComponent();
            InitializeComplete();
            _fullPathResolver = new FullPathResolver(() => Module.WorkingDir);
        }

        protected override void OnRuntimeLoad(EventArgs e)
        {
            base.OnRuntimeLoad(e);
            LoadFile();
            _NO_TRANSLATE_MailMapText.TextLoaded += MailMapFileLoaded;
        }

        private void LoadFile()
        {
            try
            {
                string path = _fullPathResolver.Resolve(".mailmap");
                if (File.Exists(path))
                {
                    _NO_TRANSLATE_MailMapText.ViewFileAsync(path!);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void SaveClick(object sender, EventArgs e)
        {
            SaveFile();
            Close();
        }

        private bool SaveFile()
        {
            try
            {
                string? fileName = _fullPathResolver.Resolve(".mailmap");

                FileInfoExtensions
                    .MakeFileTemporaryWritable(
                        fileName!, // catch NRE below
                        x =>
                        {
                            MailMapFile = _NO_TRANSLATE_MailMapText.GetText();
                            if (!MailMapFile.EndsWith(Environment.NewLine))
                            {
                                MailMapFile += Environment.NewLine;
                            }

                            File.WriteAllBytes(x, GitModule.SystemEncoding.GetBytes(MailMapFile));
                        });

                UICommands.RepoChangedNotifier.Notify();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, _cannotAccessMailmap.Text + Environment.NewLine + ex.Message,
                    _cannotAccessMailmapCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void FormMailMapFormClosing(object sender, FormClosingEventArgs e)
        {
            bool needToClose = false;

            if (!IsFileUpToDate())
            {
                switch (MessageBox.Show(this, _saveFileQuestion.Text, _saveFileQuestionCaption.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        if (SaveFile())
                        {
                            needToClose = true;
                        }

                        break;
                    case DialogResult.No:
                        needToClose = true;
                        break;
                }
            }
            else
            {
                needToClose = true;
            }

            if (!needToClose)
            {
                e.Cancel = true;
            }
        }

        private void FormMailMapLoad(object sender, EventArgs e)
        {
            if (!Module.IsBareRepository())
            {
                return;
            }

            MessageBox.Show(this, _mailmapOnlyInWorkingDirSupported.Text, _mailmapOnlyInWorkingDirSupportedCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }

        private bool IsFileUpToDate()
        {
            return MailMapFile == _NO_TRANSLATE_MailMapText.GetText();
        }

        private void MailMapFileLoaded(object sender, EventArgs e)
        {
            MailMapFile = _NO_TRANSLATE_MailMapText.GetText();
        }
    }
}
