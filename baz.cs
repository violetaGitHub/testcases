using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using log4net;

using Codice.CM.Client.Differences.Graphic;
using Codice.CM.Merge.Gui;
using Codice.CM.Merge.Gui.Semantic;
using Codice.CM.Merge.Gui.TextBox;
using Codice.CM.Merge.Gui.TextBox.SyntaxHighlight;
using Codice.CM.Merge.Gui.Semantic.DiffIconButtons;
using Codice.CM.SemanticMerge.Gui.Controller;
using Codice.CM.SemanticMerge.Gui.Model;
using Codice.CM.SemanticMerge.Gui.Model.Sync;
using XDiffGui.Options;
using XDiffGui.Semantic;

namespace Codice.CM.SemanticMerge.Gui.Merge
{
    internal interface IMoveExplanationCalculator
    {
        MoveMoveExplanation GetMoveMoveExplanation(MovedMovedConflict conflict);
    }

    internal class MergePanel :
        ISyncDeclarationListener,
        IDeclarationSynchronizer,
        ISearchActivator,
        IEditorOptionsListener,
        IMoveExplanationCalculator
    {
        internal IDiffCodeSelector SrcDiffCodeSelector
        { get { return mContributorsView.SrcDiffCodeSelector; } }

        internal IDiffCodeSelector DstDiffCodeSelector
        { get { return mContributorsView.DstDiffCodeSelector; } }

        internal RowDefinition ContributorsRow { get { return mContributorsRow; } }
        internal RowDefinition ResultRow { get { return mResultRow; } }
        internal GridSplitter ResultSplitter { get { return mResultSplitter; } }
        internal ContributorsHeaderPanel ContributorsHeaderPanel { get { return mContributorsHeaderPanel; } }

        internal MergePanel(
            ICurrentConflictUpdater currentConflictUpdater,
            IConflictResolver conflictResolver,
            IHeaderPrinter headerPrinter,
            IIconPrinter iconPrinter,
            IExplanationUpdater explanationUpdater,
            IManualEditionSaver manualEditionSaver,
            SemanticMergeSource.FileTrees fileTrees,
            SemanticToolInfo toolInfo,
            IDifferencesExecutor diffExecutor,
            ISyntaxLanguageListener syntaxLanguageListener,
            IFileNavigator fileNavigator)
        {
            mFileTrees = fileTrees;
            mToolInfo = toolInfo;

            mContributorsView = new ContributorsView(
                currentConflictUpdater,
                conflictResolver,
                this,
                headerPrinter,
                explanationUpdater,
                this,
                this,
                toolInfo,
                diffExecutor,
                fileNavigator);

            mResultView = new ResultView(
                currentConflictUpdater,
                conflictResolver,
                this,
                headerPrinter,
                iconPrinter,
                explanationUpdater,
                this,
                this,
                manualEditionSaver,
                toolInfo,
                diffExecutor,
                syntaxLanguageListener,
                fileNavigator);
        }

        internal void NotifyLicenseError(string message)
        {
            //CHANGE CASE 4 DST
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

        internal Panel GetPanel()
        {
            if (mMainPanel == null)
            {
                mMainPanel = BuildComponents();
            }

            return mMainPanel;
        }

        internal void SetFont()
        {
            mContributorsView.SetFont();
            mResultView.SetFont();
        }

        internal void SetupHeadersColors()
        {
            mContributorsHeaderPanel.SetupHeadersColors();
        }

        internal void SetSyntaxLanguage(Language language)
        {
            mContributorsView.SetSyntaxLanguage(language);
            mResultView.SetSyntaxLanguage(language);
        }

        internal void SetSrcTextBoxText(string srcText)
        {
            mContributorsView.SetSrcTextBoxText(srcText);
        }

        internal void SetBaseTextBoxText(string baseText)
        {
            mContributorsView.SetBaseTextBoxText(baseText);
        }

        internal void SetDstTextBoxText(string dstText)
        {
            mContributorsView.SetDstTextBoxText(dstText);
        }

        internal void SetResultTextBoxText(string resultText)
        {
            mResultView.SetText(resultText);
        }

        internal void SetResultTextBoxEditable()
        {
            mResultView.SetEditable();
        }

        internal string GetResultContent()
        {
            return mResultView.GetContent();
        }

        internal void SetDrawingInfo(DrawingInfo drawingInfo)
        {
            mContributorsView.SetDrawingInfo(drawingInfo);
            mResultView.SetDrawingInfo(drawingInfo);

            mDeclarationSynchronizer.SetDeclarationMappings(drawingInfo.DeclarationMappings);

            UpdateVirtualMapping(drawingInfo.Mapping);
        }

        internal void SetResultDiffIcons(List<DiffIcon> diffIcons)
        {
            mResultView.SetResultDiffIcons(diffIcons);
        }

        internal void UpdateInfoForSyncedDeclaration(
            List<Conflict> conflicts, List<Difference> srcDiffs, List<Difference> dstDiffs)
        {
            mResultView.UpdateInfoForSyncedDeclaration(conflicts, srcDiffs, dstDiffs);
        }

        internal void UpdateVirtualMapping(ThreeWayVirtualLinesMapping mapping)
        {
            mContributorsScrollView.UpdateVirtualMapping(mapping);
        }

        internal void SetCurrentConflict(
            CurrentConflictContributorDeclarationRanges contributorRanges,
            DeclarationRange resultRange, bool hasUnsolvedConflicts)
        {
            mContributorsView.SetCurrentConflict(contributorRanges, hasUnsolvedConflicts);

            mResultView.SetCurrentConflict(resultRange, hasUnsolvedConflicts);
        }

        internal void SetFileTrees(SemanticMergeSource.FileTrees fileTrees)
        {
            mFileTrees = fileTrees;
        }

        internal void Dispose()
        {
            mContributorsView.Dispose();
            mResultView.Dispose();

            if (mMainPanel == null)
                return;

            mContributorsHeaderPanel.Dispose();
            mContributorsScrollView.Dispose();
            mDeclarationSynchronizer.Dispose();
        }

        void ISyncDeclarationListener.OnSyncDeclarationClicked(
            SyncDeclarationFrom syncDeclarationFrom, int line)
        {
            ContributorDeclarationRanges contributorRanges;
            ResultDeclarationRange resultRange;

            DeclarationRangesCalculatorByLine.GetDeclarationRanges(
                mFileTrees, mContributorsScrollView.Mapping,
                syncDeclarationFrom, line,
                out contributorRanges,
                out resultRange);

            ((IDeclarationSynchronizer)this).GoToLine(contributorRanges, resultRange);
        }

        void IDeclarationSynchronizer.GoToLine(
            ContributorDeclarationRanges contributorRanges,
            ResultDeclarationRange resultRange)
        {
            DeclarationRange resultDeclarationRange = (resultRange != null) ?
                resultRange.Range : null;

            mDeclarationSynchronizer.GoToLine(contributorRanges, resultDeclarationRange);

            Codice.Tree.Declaration currentDeclaration = (resultRange != null) ?
                resultRange.Declaration : contributorRanges.Declaration;

            mResultView.SetCurrentDeclaration(currentDeclaration);
        }

        void ISearchActivator.ActivateSearch()
        {
            if (mResultView.ResultTextBox == mActiveTextBox)
            {
                mResultView.ActivateSearch();
                return;
            }
            mContributorsView.ActivateSearch(mActiveTextBox);
        }

        void ISearchActivator.SetActiveTextBox(SyntaxTextBox activeTextBox)
        {
            mActiveTextBox = activeTextBox;
        }

        void IEditorOptionsListener.OnViewWhiteSpacesChanged(bool value)
        {
            mContributorsView.SetWhitespacesVisible(value);
            mResultView.SetWhitepacesVisible(value);
        }

        void IEditorOptionsListener.OnConvertTabsToSpacesChanged(bool value)
        {
            mContributorsView.SetAutoConvertTabsToSpaces(value);
            mResultView.SetAutoConvertTabsToSpaces(value);
        }

        void IEditorOptionsListener.OnTabSpacesChanged(int value)
        {
            mContributorsView.SetTabSpaces(value);
            mResultView.SetTabSpaces(value);
        }

        void IEditorOptionsListener.OnColumnGuidesChanged(List<int> list)
        {
            mContributorsView.SetColumnGuides(list);
            mResultView.SetColumnGuides(list);
        }

        void IEditorOptionsListener.OnViewBorderLinesChanged(bool value)
        {
#warning Not implemented yet
        }

        MoveMoveExplanation IMoveExplanationCalculator.GetMoveMoveExplanation(
            MovedMovedConflict conflict)
        {
            return MoveMoveExplanationBuilder.GetMoveExplanation(
                mFileTrees.Src, mFileTrees.Base, mFileTrees.Dst, conflict);
        }

        Panel BuildComponents()
        {
            int ini = Environment.TickCount;

            Grid result = new Grid();

            mContributorsHeaderPanel = new ContributorsHeaderPanel(this, mContributorsView);
            Grid contributorsHeaderPanel = mContributorsHeaderPanel.GetPanel(mToolInfo);

            mContributorsHeaderRow = new RowDefinition();
            mContributorsHeaderRow.Height = GridLength.Auto;

            mContributorsRow = new RowDefinition();
            RowDefinition resultSplitterRow = new RowDefinition();
            resultSplitterRow.Height = GridLength.Auto;
            mResultRow = new RowDefinition();

            result.RowDefinitions.Add(mContributorsHeaderRow);
            result.RowDefinitions.Add(mContributorsRow);
            result.RowDefinitions.Add(resultSplitterRow);
            result.RowDefinitions.Add(mResultRow);

            mContributorsScrollView = new ContributorsScrollView(
                mContributorsView, this, mContributorsHeaderPanel);
            mContributorsView.SetScrollNavigator(mContributorsScrollView);

            ScrollViewer contributorsViewer = XMergeControlBuilder.Scrolls.CreateScrollViewer();
            contributorsViewer.CanContentScroll = true;
            contributorsViewer.Content = mContributorsScrollView;
            contributorsViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            contributorsViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;

            mResultSplitter = XMergeControlBuilder.Splitters.CreateHorizontalSplitter();
            mResultSplitter.ShowsPreview = true;
            mResultSplitter.Height = SPLITTER_HEIGHT;

            Panel resultPanel = mResultView.GetPanel(mContributorsRow,
                mContributorsHeaderRow, mResultRow, mResultSplitter);

            Grid.SetRow(contributorsHeaderPanel, 0);
            Grid.SetRow(contributorsViewer, 1);
            Grid.SetRow(mResultSplitter, 2);
            Grid.SetRow(resultPanel, 3);

            result.Children.Add(contributorsHeaderPanel);
            result.Children.Add(contributorsViewer);
            result.Children.Add(mResultSplitter);
            result.Children.Add(resultPanel);

            mDeclarationSynchronizer = new DeclarationSynchronizer(
                mContributorsView, mContributorsScrollView, mResultView);

            mLog.DebugFormat("BuildComponents time {0} ms", Environment.TickCount - ini);

            return result;
        }

        ContributorsView mContributorsView;
        RowDefinition mContributorsRow;

        ContributorsHeaderPanel mContributorsHeaderPanel;
        RowDefinition mContributorsHeaderRow;

        ContributorsScrollView mContributorsScrollView;
        ResultView mResultView;
        RowDefinition mResultRow;

        GridSplitter mResultSplitter;

        DeclarationSynchronizer mDeclarationSynchronizer;

        SemanticMergeSource.FileTrees mFileTrees;

        Panel mMainPanel;

        SyntaxTextBox mActiveTextBox;

        SemanticToolInfo mToolInfo;

        const double SPLITTER_HEIGHT = 5;

        static readonly ILog mLog = LogManager.GetLogger("MergePanel");
    }
}