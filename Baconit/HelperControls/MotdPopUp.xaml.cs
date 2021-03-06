﻿using BaconBackend.Helpers;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BaconBackend.Managers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Baconit.HelperControls
{
    public sealed partial class MotdPopUp : UserControl
    {
        /// <summary>
        /// Fired when the Motd box close
        /// </summary>
        public event EventHandler<EventArgs> OnHideComplete
        {
            add => _mOnHideComplete.Add(value);
            remove => _mOnHideComplete.Remove(value);
        }

        private readonly SmartWeakEvent<EventHandler<EventArgs>> _mOnHideComplete = new SmartWeakEvent<EventHandler<EventArgs>>();

        public MotdPopUp(string title, string markdownContent)
        {
            InitializeComponent();

            // Set the title and markdown
            ui_titleText.Text = title;
            ui_markdownText.Markdown = markdownContent;

            // Hide the box
            VisualStateManager.GoToState(this, "HideDialog", false);
        }

        public void ShowPopUp()
        {
            // Show it
            VisualStateManager.GoToState(this, "ShowDialog", true);

            // Sub to back button callbacks
            App.BaconMan.OnBackButton += BaconMan_OnBackButton;
        }

        private void Close_OnIconTapped(object sender, EventArgs e)
        {
            // Hide it
            VisualStateManager.GoToState(this, "HideDialog", true);

            // UnSub to back button callbacks
            App.BaconMan.OnBackButton -= BaconMan_OnBackButton;
        }

        /// <summary>
        /// Fired when the hide animation is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideDialog_Completed(object sender, object e)
        {
            _mOnHideComplete.Raise(this, new EventArgs());
        }

        /// <summary>
        /// Fired when the back button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaconMan_OnBackButton(object sender, BaconBackend.BackButtonArgs e)
        {
            if(e.IsHandled)
            {
                return;
            }

            // Close the dialog
            Close_OnIconTapped(null, null);

            // Mark as handled.
            e.IsHandled = true;
        }

        private void MarkdownText_OnMarkdownLinkTapped(object sender, UniversalMarkdown.MarkdownLinkTappedArgs e)
        {            
            try
            {
                // See if what we have is a reddit link
                var redditContent = MiscellaneousHelper.TryToFindRedditContentInLink(e.Link);

                if(redditContent != null && redditContent.Type != RedditContentType.Website)
                {
                    // If we are opening a reddit link show the content and hide hide the message.
                    App.BaconMan.ShowGlobalContent(redditContent);

                    // Hide the box
                    Close_OnIconTapped(null, null);
                }
                else
                {
                    // If we have a link show it but don't close the message.
                    App.BaconMan.ShowGlobalContent(e.Link);
                }                
            }
            catch (Exception ex)
            {
                TelemetryManager.ReportUnexpectedEvent(this, "MOTDLinkFailedToOpen", ex);
                App.BaconMan.MessageMan.DebugDia("MOTDLinkFailedToOpen", ex);
            }
        }
    }
}
