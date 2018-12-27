using System;
using System.Windows;
using System.Windows.Controls;

using GitMasterGui;
using GitMasterGui.License;
using GitMasterGui.LoginWindow;
using GitMaster.UI;
using GitMaster.UI.Web;
using GitMaster.Web.Models.Api;
using GitMasterGui.License.Download;
using GitMasterGui.Api;

namespace GitMaster.LoginWindow
{
    internal class WaitingLicensePanel : DockPanel
    {
        internal WaitingLicensePanel(
            IGitMasterRestApi restApi,
            LoginWindowLicenseDownloader.INotifier licenseDownloaderNotifier)
        {
            mRestApi = restApi;

            mLicenseDownloaderNotifier = licenseDownloaderNotifier;

            BuildComponents();
        }

        internal void DownloadLicense(string teamInvitationCode)
        {
            RunDownloadLicense(teamInvitationCode);
        }

        internal void Dispose()
        {
            if (mGetLicenseButton != null)
                mGetLicenseButton.Click -= GetLicenseButton_Click;
        }

        internal void NotifyLicenseError(string message)
        {
            Children.Clear();

            Image mascotImage = ControlBuilder.CreateImage(
                GitMasterImages.GetImage(
                GitMasterImages.ImageName.IllustrationSignupError));
            mascotImage.Width = 300;
            mascotImage.Margin = new Thickness(50, 0, 0, 0);
            mascotImage.HorizontalAlignment = HorizontalAlignment.Center;
            mascotImage.VerticalAlignment = VerticalAlignment.Center;

            WebEntriesPacker.AddMascotContentComponents(
                this, mascotImage, CreateContentErrorPanel(message));
        }

        void GetLicenseButton_Click(object sender, RoutedEventArgs e)
        {
            Children.Clear();

            BuildComponents();

            RunDownloadLicense(mTeamInvitationCodeTextBox.Text);
        }

        void RunDownloadLicense(string teamInvitationCode)
        {
            LoginWindowLicenseDownloader.Run(mRestApi, teamInvitationCode, mLicenseDownloaderNotifier);
        }

        void BuildComponents()
        {
            Image mascotImage = ControlBuilder.CreateImage(
                GitMasterImages.GetImage(
                    GitMasterImages.ImageName.IllustrationLicenseWaiting));
            mascotImage.HorizontalAlignment = HorizontalAlignment.Center;
            mascotImage.VerticalAlignment = VerticalAlignment.Center;
            mascotImage.Width = 300;
            mascotImage.Margin = new Thickness(50, 0, 0, 0);

            WebEntriesPacker.AddMascotContentComponents(
                this, mascotImage, CreateContentPanel());
        }

        Panel CreateContentPanel()
        {
            StackPanel result = new StackPanel();

            TextBlock titleTextBlock = WebControlBuilder.CreateTitle(
                GitMasterLocalization.GetString(
                    GitMasterLocalization.Name.WaitingLicensePanelTitle));
            titleTextBlock.Margin = new Thickness(0, 25, 0, 15);

            WebEntriesPacker.AddRelatedComponents(
                result,
                titleTextBlock,
                WebEntriesPacker.CreateWaitingPanel(GitMasterLocalization.GetString(
                    GitMasterLocalization.Name.DownloadingLicense)));

            return result;
        }

        TextBox mTeamInvitationCodeTextBox;
        Button mGetLicenseButton;
        LoginWindowLicenseDownloader.INotifier mLicenseDownloaderNotifier;

        IGitMasterRestApi mRestApi;
        string mSuccessfulPanelTitle;
    }
}