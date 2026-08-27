$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$viewSource = Get-Content -Raw (Join-Path $root "PlayniteIntegration\ControllerSessionManagerSettingsView.cs")
$viewXaml = Get-Content -Raw (Join-Path $root "PlayniteIntegration\ControllerSessionManagerSettingsView.xaml")
$pluginSource = Get-Content -Raw (Join-Path $root "PlayniteIntegration\ControllerSessionManagerPlugin.cs")
$testerIntegrationSource = Get-Content -Raw (Join-Path $root "Tester\TesterIntegration.cs")
if (([regex]::Matches($pluginSource, 'logger\.Info\s*\(')).Count -gt 1 -or
    $testerIntegrationSource -match 'logger\.Info\s*\(' -or
    $testerIntegrationSource -notmatch 'diagnosticLoggingEnabled\s*\(\s*\)') {
    throw "High-frequency controller, session and tester-host messages must remain behind diagnostic logging."
}
if ($viewSource -notmatch 'OnSliderTrackMouseDown' -or
    $viewSource -notmatch 'Mouse\.PreviewMouseDownEvent') {
    throw "Every settings slider must support clicking its track to jump to the selected value."
}
if ($viewXaml -notmatch '<Expander x:Name="CustomSoundsSection"' -or
    $viewXaml.IndexOf('x:Name="NotificationSoundPreviewPanel"') -gt
        $viewXaml.IndexOf('IsChecked="{Binding EnableDesktopNotificationSounds}"') -or
    $viewXaml.IndexOf('IsChecked="{Binding EnableDesktopNotificationSounds}"') -gt
        $viewXaml.IndexOf('x:Name="NotificationSoundPackSelector"')) {
    throw "Custom sounds must be collapsible and sound switches must sit between previews and the pack."
}
if ($viewXaml -notmatch 'SelectedValue="{Binding CreatorThemeUpdatePolicy}"' -or
    $viewXaml -notmatch 'Tag="Startup"' -or $viewXaml -notmatch 'Tag="Daily"' -or
    $viewXaml -notmatch 'Tag="Manual"') {
    throw "Appearance options must expose startup, daily and manual creator-design updates."
}
if ($viewSource -notmatch 'plugin\.ShowNotificationPresetPreview\s*\(\s*\)') {
    throw "Changing a notification style preset must launch its automatic preview."
}
if ($pluginSource -notmatch 'ShowDesktopNotificationPreview\s*\(\s*"connected"\s*,\s*false\s*\)' -or
    $pluginSource -notmatch 'ShowNotificationPreview\s*\(\s*"connected"\s*,\s*false\s*\)') {
    throw "The automatic notification preset preview must explicitly disable sound."
}
if (($viewXaml | Select-String -Pattern 'Click="UpdateCreatorThemesClick"' -AllMatches).Matches.Count -ne 2 -or
    $viewSource -notmatch 'ShowOperationProgress\s*\(' -or
    $viewSource -notmatch 'CreatorThemeCatalog\.Reload\s*\(\s*\)') {
    throw "Creator design updates must use the cancellable progress window and reload the selectors."
}
$profileUpdateRows = [regex]::Matches($viewXaml,
    'Click="ImportVisualProfileClick"\s*/>\s*<Button[^>]+Click="UpdateCreatorThemesClick"',
    [Text.RegularExpressions.RegexOptions]::Singleline)
if ($profileUpdateRows.Count -ne 2) {
    throw "Each visual-profile toolbar must place its single design update button after Import."
}
$presetItemStyle = [regex]::Match($viewXaml,
    '<Style x:Key="AppearancePresetItemStyle"[\s\S]*?</Style>\s*<DataTemplate x:Key="AppearancePresetItemTemplate">').Value
if ($presetItemStyle -notmatch 'Property="IsSelected" Value="True"' -or
    $presetItemStyle -notmatch 'Property="Background" Value="\{DynamicResource Narian\.Accent\}"' -or
    $presetItemStyle -notmatch 'Property="Foreground" Value="\{DynamicResource Narian\.AccentOn\}"') {
    throw "Appearance preset selectors must use the active settings accent and its contrast color."
}
$presetItemTemplate = [regex]::Match($viewXaml,
    '<DataTemplate x:Key="AppearancePresetItemTemplate">[\s\S]*?</DataTemplate>').Value
if ($presetItemTemplate -notmatch 'Binding="\{Binding IsSelected, RelativeSource=\{RelativeSource AncestorType=\{x:Type ComboBoxItem\}\}\}"' -or
    $presetItemTemplate -notmatch 'TargetName="PresetLabel" Property="Foreground" Value="\{DynamicResource Narian\.AccentOn\}"') {
    throw "The selected preset label must explicitly inherit the active accent contrast color."
}

[Reflection.Assembly]::LoadFrom("C:\Playnite\Playnite.SDK.dll") | Out-Null
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase

$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Release\ControllerSessionManager.dll"))
$catalogType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.CreatorThemeCatalog", $true)
$configureCatalogArgs = [object[]]@([string](Join-Path $root "obj\EmptyCreatorPackPlugin"))
$catalogType.GetMethod("Configure", [type[]]@([string])).Invoke($null, $configureCatalogArgs) | Out-Null
$definitionType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.CreatorThemeDefinition", $true)
$definition = [Activator]::CreateInstance($definitionType)
$definition.Id = "example.creator"
$definition.Name = "Example Creator Design"
$definition.Author = "Test Author"
$definition.Version = "1.0.0"
$definition.Description = "Test-only creator design."
$definition.Directory = Join-Path $root "obj\TestCreatorPack"
$definition.Notification = [Collections.Generic.Dictionary[string, object]]::new()
$definition.Notification.Add("NotificationBackgroundColor", "#FF112233")
$definition.Overlay = [Collections.Generic.Dictionary[string, object]]::new()
$definition.Overlay.Add("OverlayCardColor", "#FF334455")
$definition.Overlay.Add("OverlayLayoutMode", "Alert")
$definition.Overlay.Add("OverlayBlockOrder", "Incident,Title,ControllerName,Metadata,Instruction,Status")
$definition.Overlay.Add("OverlayStatusInMetadata", $true)
$definition.Overlay.Add("OverlayShowIncidentBadge", $true)
$catalogFlags = [Reflection.BindingFlags]"Static,NonPublic"
$definitions = $catalogType.GetField("Definitions", $catalogFlags).GetValue($null)
$viewType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettingsView", $true)
$pluginType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerPlugin", $true)
$settingsType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", $true)
$constructor = $viewType.GetConstructor(@($pluginType))
if ($null -eq $constructor) {
    throw "Settings view test constructor was not found."
}

$view = $constructor.Invoke(@($null))
if ($null -eq $view.FindName("DesktopDesignExpander") -or
    $null -eq $view.FindName("FullscreenDesignExpander") -or
    $null -eq $view.FindName("OverlayDesignExpander")) {
    throw "The creator-design update surfaces were not constructed."
}
$selector = $view.FindName("NotificationSoundPackSelector")
if ($null -eq $selector) {
    throw "Settings view XAML did not create the notification sound pack selector."
}
if ($view.FindName("CustomSoundsSection") -isnot [Windows.Controls.Expander]) {
    throw "Custom Sounds must be an Expander."
}

$settings = [Activator]::CreateInstance($settingsType)
$settings.NotificationSoundPack = "5_Minimal_Soft"
$settings.NotificationStylePreset = "Custom"
$settings.DesktopNotificationStylePreset = "Custom"
$settings.SaveCurrentNotificationStyleAsCustom()
$settings.SaveCurrentDesktopNotificationStyleAsCustom()
$settings.NotificationWidth += 7
$settings.DesktopNotificationWidth += 9
$unsavedFullscreenWidth = $settings.NotificationWidth
$unsavedDesktopWidth = $settings.DesktopNotificationWidth
$view.DataContext = $settings
# DataContext assignment reloads the disk catalog. Reinsert the in-memory fixture because
# PowerShell 7 cannot instantiate the plugin's .NET Framework JavaScriptSerializer reliably.
$definitions.Add($definition.Id, $definition)
$viewFlags = [Reflection.BindingFlags]"Instance,NonPublic"
foreach ($methodName in @(
    "BuildNotificationStylePresetChips", "BuildNotificationPresetSelectors",
    "BuildOverlayStylePresetChips", "BuildOverlayPresetSelector")) {
    $viewType.GetMethod($methodName, $viewFlags).Invoke($view, $null) | Out-Null
}
$window = New-Object System.Windows.Window
$window.Content = $view
$window.Show()
$window.UpdateLayout()
if ($view.FindName("DesktopAlertsExpander").IsExpanded -or
    $view.FindName("DesktopDesignExpander").IsExpanded -or
    $view.FindName("DesktopAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("FullscreenAlertsExpander").IsExpanded -or
    $view.FindName("FullscreenDesignExpander").IsExpanded -or
    $view.FindName("FullscreenAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("OverlayDesignExpander").IsExpanded -or
    $view.FindName("OverlayAppearanceLayoutExpander").IsExpanded) {
    throw "Notification and overlay child sections must start collapsed."
}
$overlayLayout = $view.FindName("OverlayEditorLayoutGrid")
$overlaySettingsScroll = $view.FindName("OverlaySettingsScrollViewer")
$overlayPreviewPane = $view.FindName("OverlayPreviewPane")
$overlayPreviewViewport = $view.FindName("OverlayPreviewViewport")
function Test-LogicalAncestor($ancestor, $element) {
    while ($null -ne $element) {
        if ([object]::ReferenceEquals($ancestor, $element)) { return $true }
        $element = [Windows.LogicalTreeHelper]::GetParent($element)
    }
    return $false
}
if ($null -eq $overlayLayout -or $overlayLayout.ColumnDefinitions.Count -ne 3 -or
    $overlayLayout.ColumnDefinitions[0].Width.GridUnitType -ne [Windows.GridUnitType]::Star -or
    $overlayLayout.ColumnDefinitions[2].Width.GridUnitType -ne [Windows.GridUnitType]::Star -or
    $overlayLayout.ColumnDefinitions[2].MinWidth -lt 360 -or
    $null -eq $overlaySettingsScroll -or
    -not (Test-LogicalAncestor $overlaySettingsScroll $view.FindName("OverlayDesignExpander")) -or
    -not (Test-LogicalAncestor $overlaySettingsScroll $view.FindName("OverlayStyleEditor")) -or
    $view.FindName("OverlayDesignExpander").Margin.Bottom -lt 8 -or
    $null -eq $overlayPreviewPane -or
    (Test-LogicalAncestor $overlaySettingsScroll $overlayPreviewPane) -or
    [Windows.Controls.Grid]::GetColumn($overlayPreviewPane) -ne 2 -or
    $overlayPreviewPane.HorizontalAlignment -ne [Windows.HorizontalAlignment]::Stretch -or
    $overlayPreviewPane.VerticalAlignment -ne [Windows.VerticalAlignment]::Center -or
    $null -eq $overlayPreviewViewport -or
    $overlayPreviewViewport -is [Windows.Controls.ScrollViewer]) {
    throw "The overlay preview must remain in a fixed, non-scrolling, wider right-hand pane " +
        "(layout=$($null -ne $overlayLayout), columns=$($overlayLayout.ColumnDefinitions.Count), " +
        "previewMin=$($overlayLayout.ColumnDefinitions[2].MinWidth), scroll=$($null -ne $overlaySettingsScroll), " +
        "designInScroll=$(Test-LogicalAncestor $overlaySettingsScroll $view.FindName('OverlayDesignExpander')), " +
        "editorInScroll=$(Test-LogicalAncestor $overlaySettingsScroll $view.FindName('OverlayStyleEditor')), " +
        "previewInScroll=$(Test-LogicalAncestor $overlaySettingsScroll $overlayPreviewPane), " +
        "column=$([Windows.Controls.Grid]::GetColumn($overlayPreviewPane)), " +
        "alignment=$($overlayPreviewPane.HorizontalAlignment)/$($overlayPreviewPane.VerticalAlignment), " +
        "viewport=$($overlayPreviewViewport.GetType().Name))."
}
if ($view.FindName("DesktopAdvancedDesignExpander").Visibility -ne [Windows.Visibility]::Collapsed -or
    $view.FindName("FullscreenAdvancedDesignExpander").Visibility -ne [Windows.Visibility]::Collapsed -or
    $view.FindName("OverlayAdvancedDesignExpander").Visibility -ne [Windows.Visibility]::Collapsed) {
    throw "Advanced Design must remain hidden from the normal settings interface."
}
if ($settings.NotificationStylePreset -ne "Custom" -or
    $settings.DesktopNotificationStylePreset -ne "Custom" -or
    $settings.NotificationWidth -ne $unsavedFullscreenWidth -or
    $settings.DesktopNotificationWidth -ne $unsavedDesktopWidth) {
    throw "Opening the settings view applied a notification preset or discarded unsaved custom values."
}
$desktopPresetSelector = $view.FindName("DesktopNotificationPresetSelector")
$fullscreenPresetSelector = $view.FindName("FullscreenNotificationPresetSelector")
$overlayPresetSelector = $view.FindName("OverlayPresetSelector")
if ($null -eq $desktopPresetSelector -or $null -eq $fullscreenPresetSelector -or
    $null -eq $overlayPresetSelector) {
    throw "The destination-specific appearance preset selectors were not created."
}
if ($desktopPresetSelector.Items.Count -ne 10 -or $fullscreenPresetSelector.Items.Count -ne 10 -or
    $overlayPresetSelector.Items.Count -ne 9) {
    throw "The grouped appearance selectors do not contain all plugin, creator, and custom presets."
}
if ($desktopPresetSelector.Items[0].Key -ne "Custom" -or
    $fullscreenPresetSelector.Items[0].Key -ne "Custom" -or
    $overlayPresetSelector.Items[0].Key -ne "Custom" -or
    @($desktopPresetSelector.Items | Where-Object { $_.IsHeader }).Count -ne 2 -or
    @($fullscreenPresetSelector.Items | Where-Object { $_.IsHeader }).Count -ne 2 -or
    @($overlayPresetSelector.Items | Where-Object { $_.IsHeader }).Count -ne 2 -or
    @($desktopPresetSelector.Items | Where-Object { $_.IsSelectable }).Count -ne 8) {
    throw "Appearance presets are not grouped into plugin, creator, and custom designs."
}
$notificationPluginPresets = $view.FindName("NotificationPluginPresetChips")
$notificationCreatorPresets = $view.FindName("NotificationCreatorPresetChips")
$notificationCustomPresets = $view.FindName("NotificationCustomPresetChips")
$overlayPluginPresets = $view.FindName("OverlayPluginPresetChips")
$overlayCreatorPresets = $view.FindName("OverlayCreatorPresetChips")
$overlayCustomPresets = $view.FindName("OverlayCustomPresetChips")
if ($notificationPluginPresets.Children.Count -ne 6 -or
    $notificationCreatorPresets.Children.Count -ne 1 -or
    $notificationCustomPresets.Children.Count -ne 1 -or
    $overlayPluginPresets.Children.Count -ne 5 -or
    $overlayCreatorPresets.Children.Count -ne 1 -or
    $overlayCustomPresets.Children.Count -ne 1) {
    throw "Plugin, creator, and custom appearance presets are not separated correctly."
}
$creatorCard = $notificationCreatorPresets.Children[0]
if ($creatorCard.Tag -ne "example.creator" -or
    $creatorCard.Content.Children[1].Text -ne "Test Author") {
    throw "Creator preset cards do not expose their design attribution."
}
if ($selector.Items.Count -lt 1) {
    throw "Settings view did not populate the notification sound packs."
}
if ($selector.SelectedValue -ne $settings.NotificationSoundPack) {
    throw "Settings view did not select the configured notification sound pack."
}
if ($settings.NotificationSoundPack -ne "5_Minimal_Soft") {
    throw "Opening the settings view replaced the saved notification sound pack."
}

$desktopPresetSelector.SelectedItem = @($desktopPresetSelector.Items | Where-Object { $_.Key -eq "example.creator" })[0]
$window.UpdateLayout()
if ($settings.DesktopNotificationStylePreset -ne "example.creator" -or
    $settings.NotificationStylePreset -eq "example.creator") {
    throw "Selecting a desktop notification design also changed the fullscreen destination " +
        "(desktop=$($settings.DesktopNotificationStylePreset), fullscreen=$($settings.NotificationStylePreset))."
}
if ($view.FindName("DesktopNotificationStyleEditor").IsEnabled -or
    -not $view.FindName("FullscreenNotificationStyleEditor").IsEnabled -or
    -not $view.FindName("NotificationAudioEditor").IsEnabled -or
    $view.FindName("NotificationSoundPackSelector").IsEnabled -or
    $view.FindName("NotificationSoundPackSelector").Visibility -ne [Windows.Visibility]::Collapsed -or
    $view.FindName("CustomSoundsSection").Visibility -ne [Windows.Visibility]::Collapsed -or
    -not $view.FindName("NotificationSoundPreviewPanel").IsEnabled -or
    -not $view.FindName("NotificationSoundOptionsPanel").IsEnabled -or
    $view.FindName("CopyFullscreenToDesktopButton").IsEnabled -or
    $view.FindName("CopyDesktopToFullscreenButton").IsEnabled -or
    $view.FindName("DesktopAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("DesktopNotificationStyleEditor").Opacity -gt 0.5) {
    throw "Creator notification designs must lock only appearance, pack selection, and custom files " +
        "(desktop=$($view.FindName('DesktopNotificationStyleEditor').IsEnabled), " +
        "fullscreen=$($view.FindName('FullscreenNotificationStyleEditor').IsEnabled), " +
        "audio=$($view.FindName('NotificationAudioEditor').IsEnabled), " +
        "pack=$($view.FindName('NotificationSoundPackSelector').IsEnabled)/$($view.FindName('NotificationSoundPackSelector').Visibility), " +
        "custom=$($view.FindName('CustomSoundsSection').Visibility), " +
        "preview=$($view.FindName('NotificationSoundPreviewPanel').IsEnabled), " +
        "options=$($view.FindName('NotificationSoundOptionsPanel').IsEnabled), " +
        "opacity=$($view.FindName('DesktopNotificationStyleEditor').Opacity), " +
        "flags=$($settings.CanEditDesktopNotificationStyle)/$($settings.CanEditFullscreenNotificationStyle)/$($settings.CanEditNotificationAudio))."
}
$desktopPresetSelector.SelectedItem = @($desktopPresetSelector.Items | Where-Object { $_.Key -eq "Custom" })[0]
$window.UpdateLayout()
if (-not $view.FindName("DesktopNotificationStyleEditor").IsEnabled -or
    -not $view.FindName("NotificationAudioEditor").IsEnabled -or
    -not $view.FindName("NotificationSoundPackSelector").IsEnabled -or
    $view.FindName("NotificationSoundPackSelector").Visibility -ne [Windows.Visibility]::Visible -or
    $view.FindName("CustomSoundsSection").Visibility -ne [Windows.Visibility]::Visible -or
    $view.FindName("DesktopNotificationStyleEditor").Opacity -lt 0.9) {
    throw "Returning to Custom did not unlock notification appearance and audio editing."
}
$overlayPresetSelector.SelectedItem = @($overlayPresetSelector.Items | Where-Object { $_.Key -eq "example.creator" })[0]
$window.UpdateLayout()
if ($settings.OverlayStylePreset -ne "example.creator" -or
    $view.FindName("OverlayStyleEditor").IsEnabled -or
    $view.FindName("OverlayAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("OverlayStyleEditor").Opacity -gt 0.5) {
    throw "Creator overlay designs did not lock the overlay appearance editor " +
        "(preset=$($settings.OverlayStylePreset), editor=$($view.FindName('OverlayStyleEditor').IsEnabled), " +
        "expanded=$($view.FindName('OverlayAppearanceLayoutExpander').IsExpanded), " +
        "opacity=$($view.FindName('OverlayStyleEditor').Opacity))."
}
$overlayPresetSelector.SelectedItem = @($overlayPresetSelector.Items | Where-Object { $_.Key -eq "Custom" })[0]
$window.UpdateLayout()
if (-not $view.FindName("OverlayStyleEditor").IsEnabled) {
    throw "Returning the overlay to Custom did not unlock its appearance editor."
}

$settings.NotificationSoundPack = "4_Retro_Arcade"
if ($selector.SelectedValue -ne $settings.NotificationSoundPack) {
    throw "Settings view did not follow an externally changed notification sound pack."
}
$selector.SelectedIndex = 6
$window.UpdateLayout()
if ($settings.NotificationSoundPack -ne "7_Handheld_Haptic") {
    $refreshField = $viewType.GetField(
        "refreshingNotificationSoundPackSelection", [Reflection.BindingFlags]"Instance,NonPublic")
    throw "Selecting a notification sound pack did not update the settings model " +
        "(selector=$($selector.SelectedValue), settings=$($settings.NotificationSoundPack), " +
        "refreshing=$($refreshField.GetValue($view)))."
}

$desktopImageRow = $view.FindName("DesktopBackgroundImagePickerRow")
$fullscreenImageRow = $view.FindName("FullscreenBackgroundImagePickerRow")
$overlayImageRow = $view.FindName("OverlayBackgroundImagePickerRow")
if ($desktopImageRow.IsEnabled -or $fullscreenImageRow.IsEnabled -or $overlayImageRow.IsEnabled) {
    throw "Background image selectors must be disabled while background images are disabled " +
        "(desktop=$($desktopImageRow.IsEnabled), fullscreen=$($fullscreenImageRow.IsEnabled); " +
        "settings=$($settings.DesktopNotificationUseBackgroundImage)/$($settings.NotificationUseBackgroundImage); " +
        "bindings=$($desktopImageRow.GetBindingExpression([System.Windows.UIElement]::IsEnabledProperty).Status)/" +
        "$($fullscreenImageRow.GetBindingExpression([System.Windows.UIElement]::IsEnabledProperty).Status))."
}
$settings.DesktopNotificationUseBackgroundImage = $true
$settings.NotificationUseBackgroundImage = $true
$settings.OverlayUseBackgroundImage = $true
if (-not $desktopImageRow.IsEnabled -or -not $fullscreenImageRow.IsEnabled -or
    -not $overlayImageRow.IsEnabled) {
    throw "Background image selectors did not follow their enable settings " +
        "(desktop=$($desktopImageRow.IsEnabled), fullscreen=$($fullscreenImageRow.IsEnabled), overlay=$($overlayImageRow.IsEnabled); " +
        "presets=$($settings.DesktopNotificationStylePreset)/$($settings.NotificationStylePreset)/$($settings.OverlayStylePreset); " +
        "values=$($settings.DesktopNotificationUseBackgroundImage)/$($settings.NotificationUseBackgroundImage)/$($settings.OverlayUseBackgroundImage); " +
        "editable=$($settings.CanEditDesktopNotificationStyle)/$($settings.CanEditFullscreenNotificationStyle)/$($settings.CanEditOverlayStyle); " +
        "suppress=$($viewType.GetField('suppressingStylePresetMark',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($view)))."
}
$window.Close()

$reopenedView = $constructor.Invoke(@($null))
$reopenedView.DataContext = $settings
$reopenedWindow = New-Object System.Windows.Window
$reopenedWindow.Content = $reopenedView
$reopenedWindow.Show()
$reopenedWindow.UpdateLayout()
$reopenedSelector = $reopenedView.FindName("NotificationSoundPackSelector")
if ($settings.NotificationSoundPack -ne "7_Handheld_Haptic" -or
    $reopenedSelector.SelectedValue -ne "7_Handheld_Haptic") {
    throw "Reopening the settings view did not retain the selected notification sound pack."
}
$reopenedWindow.Close()

Write-Host "Settings view construction and sound pack binding tests passed."
