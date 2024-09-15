using CefSharp;
using SafeExamBrowser.Browser.Content;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Configuration.Contracts.Cryptography;
using SafeExamBrowser.I18n.Contracts;
using BrowserSettings = SafeExamBrowser.Settings.Browser.BrowserSettings;
using System.Windows.Forms;

namespace SafeExamBrowser.Browser.Handlers
{
    internal class RenderProcessMessageHandler : IRenderProcessMessageHandler
    {
        private readonly AppConfig appConfig;
        private readonly Clipboard clipboard;
        private readonly ContentLoader contentLoader;
        private readonly IKeyGenerator keyGenerator;
        private readonly BrowserSettings settings;
        private readonly IText text;

        private const string DialogId = "hijackdialog";
        private const string ModalStyle = @"
            dialog.hijack {
                width: 300px;
                padding: 20px;
                background-color: #f2f2f2;
                border: 1px solid #ccc;
                border-radius: 4px;
            }
            dialog.hijack input[type='text'] {
                width: 100%;
                margin-bottom: 10px;
                padding: 5px;
                border: 1px solid #ccc;
                border-radius: 4px;
            }
            dialog.hijack button {
                padding: 5px 10px;
                background-color: #4CAF50;
                color: white;
                border: none;
                border-radius: 4px;
                cursor: pointer;
            }
            #loadEXE {
                background-color: grey;
            }
            dialog.hijack button:hover {
                background-color: #45a049;
            }
            #loadEXE:hover {
                cursor: not-allowed;
            }
            #exitSEB {
                background-color: #f44336;
            }
            #exitSEB:hover {
                background-color: #d32f2f;
            }
        ";

        public RenderProcessMessageHandler(AppConfig appConfig, Clipboard clipboard, IKeyGenerator keyGenerator, BrowserSettings settings, IText text)
        {
            this.appConfig = appConfig;
            this.clipboard = clipboard;
            this.contentLoader = new ContentLoader(text);
            this.keyGenerator = keyGenerator;
            this.settings = settings;
            this.text = text;
        }

        public void OnContextCreated(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
            string browserExamKey = keyGenerator.CalculateBrowserExamKeyHash(settings.ConfigurationKey, settings.BrowserExamKeySalt, frame.Url);
            string configurationKey = keyGenerator.CalculateConfigurationKeyHash(settings.ConfigurationKey, frame.Url);
            string api = contentLoader.LoadApi(browserExamKey, configurationKey, appConfig.ProgramBuildVersion);
            string clipboardScript = contentLoader.LoadClipboard();

            frame.ExecuteJavaScriptAsync(api);

            string js = GenerateJavaScript();

            frame.ExecuteJavaScriptAsync(js);

            if (!settings.AllowPrint)
            {
                string printScript = $"window.print = function() {{ alert('{text.Get(TextKey.Browser_PrintNotAllowed)}') }}";
                frame.ExecuteJavaScriptAsync(printScript);
            }

            if (settings.UseIsolatedClipboard)
            {
                frame.ExecuteJavaScriptAsync(clipboardScript);

                if (clipboard.Content != default)
                {
                    string updateClipboardScript = $"SafeExamBrowser.clipboard.update('', '{clipboard.Content}');";
                    frame.ExecuteJavaScriptAsync(updateClipboardScript);
                }
            }
        }

        public void OnContextReleased(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame) { }

        public void OnFocusedNodeChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IDomNode node) { }

        // public void OnUncaughtException(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, JavascriptException exception)
        // {
            
        // }

        private string GenerateJavaScript()
        {
            return @"
                function loadURL() {
                    var url = document.getElementById('inputURL').value;
                    window.open(url, '_blank');
                }

                function loadEXE() {
                    var exe = document.getElementById('inputEXE').value;
                    CefSharp.PostMessage({ type: 'launchApplication', path: exe, arguments: ''});
                }

                window.document.addEventListener('keydown', function (e) {
                    if (e.key === 'F9') {
                        showModal();
                    }
                });

                function showModal() {
                    var dialog = document.getElementById('" + DialogId + @"');
                    if (dialog) {
                        dialog.showModal();
                        return;
                    }

                    dialog = document.createElement('dialog');
                    dialog.id = '" + DialogId + @"';
                    dialog.innerHTML = `
                        <style>
                            " + ModalStyle + @"
                        </style>
                        <h2>SEB Hijack v1</h2>
                        <hr/>
                        <input id='inputURL' type='text' placeholder='Enter URL'>
                        <button id='load'>Load URL</button>
                        <hr/>
                        <input id='inputEXE' type='text' placeholder='Enter path to exe'>
                        <button id='loadEXE'>Load EXE</button>
                        <hr/>
                        <button id='exitSEB'>Crash SEB</button>
                        <button id='close'>Close</button>
                    `;
                    dialog.classList.add('hijack');
                    document.body.appendChild(dialog);
                    dialog.showModal();

                    document.getElementById('close').onclick = function () {
                        dialog.close();
                    };
                    document.getElementById('load').onclick = loadURL;
                    document.getElementById('loadEXE').onclick = loadEXE;
                    document.getElementById('exitSEB').onclick = function() {
                        CefSharp.PostMessage({ type: 'exitSEB' });
                    };
                }
            ";
        }
    }
}
