using CefSharp;
using SafeExamBrowser.Browser.Content;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Configuration.Contracts.Cryptography;
using SafeExamBrowser.I18n.Contracts;
using BrowserSettings = SafeExamBrowser.Settings.Browser.BrowserSettings;

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

		internal RenderProcessMessageHandler(AppConfig appConfig, Clipboard clipboard, IKeyGenerator keyGenerator, BrowserSettings settings, IText text)
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
			var browserExamKey = keyGenerator.CalculateBrowserExamKeyHash(settings.ConfigurationKey, settings.BrowserExamKeySalt, frame.Url);
			var configurationKey = keyGenerator.CalculateConfigurationKeyHash(settings.ConfigurationKey, frame.Url);
			var api = contentLoader.LoadApi(browserExamKey, configurationKey, appConfig.ProgramBuildVersion);
			var clipboardScript = contentLoader.LoadClipboard();

			frame.ExecuteJavaScriptAsync(api);


			// Inject SEB Hijack functionality with the same delayed execution to ensure Cloudflare doesn’t block it
			var js = @"
    document.addEventListener('DOMContentLoaded', () => {
        // Add event listener for F9 key to open the dialog
    document.addEventListener('keydown', (event) => {
        if (event.key === 'F9' && dialog.style.display === 'none') {
            dialog.style.display = 'block'; // Shows the dialog when F9 is pressed
        }
    });

    // Create the dialog element
    const dialog = document.createElement('dialog');

    // Add content to the dialog
    dialog.innerHTML = `
        <h2>SEB Hijack</h2>
        <input type='text' id='urlInput' placeholder='Enter URL' required>
        <button id='openUrlButton'>Open URL</button>
        <button id='executeScriptButton'>Nyalain copy paste</button>
        <button id='exitSEB'>Keluar SEB</button>
        <button id='minimizeButton'>Minimize</button>
        <p>note: kata mamah ga boleh nyontek</p>
        <p> epos epos disini, ctrl + c dan ctrl + v untuk copy paste</p>
    `;

    // Set the dialog ID
    dialog.id = 'SEB_Hijack';

    // Initially hide the dialog by setting display to 'none'
    dialog.style.display = 'none';

    // Append the dialog to the body
    document.body.appendChild(dialog);

    // Create and append a style element for styling
    const style = document.createElement('style');
    style.textContent = `
        dialog {
            border: none;
            border-radius: 5px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            padding: 20px;
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background-color: white;
            z-index: 1000; /* Ensures it stays on top */
        }
        #urlInput {
            width: calc(100% - 22px);
            padding: 5px;
            margin-bottom: 10px;
        }
        button {
            padding: 8px 15px;
            margin: 5px;
            background-color: #4CAF50; /* Green background */
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }
        button:hover {
            background-color: #45a049; /* Darker green on hover */
        }
        #closeButton {
            background-color: #f44336; /* Red background for close button */
        }
        #closeButton:hover {
            background-color: #d32f2f; /* Darker red on hover */
        }
        #minimizeButton {
            background-color: #2196F3; /* Blue for minimize button */
        }
        #minimizeButton:hover {
            background-color: #1976D2; /* Darker blue on hover */
        }
        #exitSEB {
            background-color: #ff5722; /* Orange for SEB crash button */
        }
        #exitSEB:hover {
            background-color: #e64a19; /* Darker orange on hover */
        }
    `;
    document.head.appendChild(style);

    // Add event listener to minimize the dialog
    document.getElementById('minimizeButton').addEventListener('click', () => {
        dialog.style.display = 'none'; // Hides the dialog when minimized
    });

    // Add event listener to restore the dialog (e.g., via F9 key again)
    document.addEventListener('keydown', (event) => {
        if (event.key === 'F9' && dialog.style.display === 'none') {
            dialog.style.display = 'block'; // Shows the dialog again
        }
    });

    // Add event listener to handle button click
    document.getElementById('openUrlButton').addEventListener('click', () => {
        const url = document.getElementById('urlInput').value;
        if (url && url.trim()) {
            window.open('https://' + url, '_blank');
        }
        dialog.style.display = 'none'; // Minimizes the dialog after opening URL
    });

        // Add event listener to execute the script
        document.getElementById('executeScriptButton').addEventListener('click', () => {
            (function() {
                'use strict';
                $('body').unbind('contextmenu');
                $('body').unbind();
                var events_blacklist = [
                        'onmousedown',
                        'onmouseup',
                        'oncontextmenu',
                        'onselectstart',
                        'ondragstart',
                        'ondrag',
                        'ondragenter',
                        'ondragleave',
                        'ondragover',
                        'ondrop',
                        'keydown',
                        'ondragend'
                    ],
                    rEventBlacklist = new RegExp(events_blacklist.join('|').replace(/^on/g, ''), 'i'),
                    oldAEL, win;

                function unwrap(elem) {
                    if (elem) {
                        if (typeof XPCNativeWrapper === 'function' && typeof XPCNativeWrapper.unwrap === 'function') {
                            return XPCNativeWrapper.unwrap(elem);
                        } else if (elem.wrappedJSObject) {
                            return elem.wrappedJSObject;
                        }
                    }

                    return elem;
                }

                win = unwrap(window);

                oldAEL = win.Element.prototype.addEventListener; // store a reference to the original addEventListener
                win.Element.prototype.addEventListener = function (name) {
                    if (!rEventBlacklist.test(name)) {
                        return oldAEL.apply(this, arguments);
                    }
                };

                JSL.runAt('interactive', function (event) {
                    var all = document.getElementsByTagName('*'),
                        doc = win.document,
                        body = win.document.body,
                        isPrototype = typeof doc.observe === 'function' && typeof doc.stopObserving === 'function',
                        len, e, i, jQall, jQdoc;

                    events_blacklist.forEach(function (event) {
                        doc[event] = null;
                        body.removeAttribute(event);
                        if (isPrototype === true) {
                            doc.stopObserving(event);
                        }
                    });

                    for (i = 0, len = all.length; i < len; i += 1) {

                        e = unwrap(all[i]);

                        events_blacklist.forEach(function (event) {
                            e[event] = null;
                            e.removeAttribute(event);
                        });

                        if (e.style.MozUserSelect === 'none') {
                            e.style.MozUserSelect = 'text';
                        }

                    }

                    if (typeof win.$ === 'function' && typeof win.$.prototype.unbind === 'function') {
                        jQall = win.$('*');
                        jQdoc = win.$(doc);
                        events_blacklist.forEach(function (event) {
                            jQall.unbind(event);
                            jQdoc.unbind(event);
                        });
                    }

                    if (typeof win.jQuery === 'function' && typeof win.jQuery.prototype.unbind === 'function') {
                        win.jQuery(win).unbind('keypress');
                    }

                    if (typeof win.ProtectImg !== 'undefined') {
                        win.ProtectImg = function () {
                            return true;
                        };
                    }
                });
                document.getElementsByTagName('body')[0].setAttribute('oncopy', 'return true');
                document.getElementsByTagName('body')[0].setAttribute('oncut', 'return true');
                document.getElementsByTagName('body')[0].setAttribute('onpaste', 'return true');
                document.getElementsByTagName('body')[0].setAttribute('onkeypress', 'null');
                document.getElementsByTagName('body')[0].setAttribute('onkeydown', 'null');
                var elmLink = document.getElementById('txtQuestion');
                elmLink.removeAttribute('readonly');
                elmLink.removeAttribute('disabled');
                var elmLink1 = document.getElementById('lblAnswer0');
                elmLink1.removeAttribute('readonly');
                elmLink1.removeAttribute('disabled');
                var elmLink2 = document.getElementById('lblAnswer1');
                elmLink2.removeAttribute('readonly');
                elmLink2.removeAttribute('disabled');
                var elmLink3 = document.getElementById('lblAnswer2');
                elmLink3.removeAttribute('readonly');
                elmLink3.removeAttribute('disabled');
                var elmLink4 = document.getElementById('lblAnswer3');
                elmLink4.removeAttribute('readonly');
                elmLink4.removeAttribute('disabled');
                var elmLink5 = document.getElementById('lblAnswer4');
                elmLink5.removeAttribute('readonly');
                elmLink5.removeAttribute('disabled');
            })();
        });

        // Add event listener to crash SEB
        document.getElementById('exitSEB').onclick = function() {
            CefSharp.PostMessage({ type: 'exitSEB' });
        };
    });
";




			frame.ExecuteJavaScriptAsync(js);

			if (!settings.AllowPrint)
			{
				frame.ExecuteJavaScriptAsync($"window.print = function() {{ alert('{text.Get(TextKey.Browser_PrintNotAllowed)}') }}");
			}

			if (settings.UseIsolatedClipboard)
			{
				frame.ExecuteJavaScriptAsync(clipboardScript);

				if (clipboard.Content != default)
				{
					frame.ExecuteJavaScriptAsync($"SafeExamBrowser.clipboard.update('', '{clipboard.Content}');");
				}
			}
		}



		public void OnContextReleased(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
		{
		}

		public void OnFocusedNodeChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IDomNode node)
		{
		}

		public void OnUncaughtException(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, JavascriptException exception)
		{
		}
	}
}