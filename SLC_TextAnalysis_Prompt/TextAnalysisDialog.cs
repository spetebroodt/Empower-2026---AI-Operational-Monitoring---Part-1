using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Linq;

namespace TextAnalysisPrompt
{
    public class TextAnalysisDialog : Dialog
    {
        private TextBox _promptTextBox;
        public string Prompt 
        { 
            get { return _promptTextBox?.Text ?? ""; } 
            set { _promptTextBox.Text = value; } 
        }

        private TextBox _inputTextBox;
        public string Input 
        { 
            get { return _inputTextBox?.Text ?? ""; }
            set { _inputTextBox.Text = value; }
        }

        private FileSelector _fileSelector;
        public string FilePath 
        { 
            get { return _fileSelector?.UploadedFilePaths.Any() ?? false ? _fileSelector.UploadedFilePaths.First() : ""; }
        }

        private Button _runButton;

        public event EventHandler Accepted;
        public event EventHandler Cancelled;

        public TextAnalysisDialog(IEngine engine) : base(engine)
        {
            Title = "Text Analysis Prompt";

            var promptLabel = new Label("Prompt");
            _promptTextBox = new TextBox()
            {
                MinWidth = 700,
                Height = 300,
                IsMultiline = true                
            };

            var inputLabel = new Label("Input");
            _inputTextBox = new TextBox()
            {
                MinWidth = 700,
                Height = 300,
                IsMultiline = true,
                IsReadOnly = true
            };           

            _runButton = new Button("Run");
            _runButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

            _fileSelector = new FileSelector();

            AddWidget(_fileSelector, 0, 0);

            AddWidget(promptLabel, 1, 0);
            AddWidget(_promptTextBox, 1, 1);
                                  
            AddWidget(_runButton, 3, 0);
        }        
    }
}
