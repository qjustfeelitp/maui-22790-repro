using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui22790;

public static class MauiProgram
{
    private const bool Workaround = false;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
           .UseMauiApp<App>()
           .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        if (Workaround)
        {
#if WINDOWS
            builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<NavigationPage, ExtendedNavigationViewHandler>());
            DisableTextServices();
#endif
        }

        return builder.Build();
    }

    /// <summary>
    /// Delete after this is done https://github.com/microsoft/microsoft-ui-xaml/issues/9070
    /// Default for <see cref="ITextInput.IsSpellCheckEnabled"/> and <see cref="ITextInput.IsTextPredictionEnabled"/> in WinUI is true and for MAUI it also defaults to true.
    /// </summary>
    private static void DisableTextServices()
    {
        // the discarded action calls UpdateIsSpellCheckEnabled but to protect ourselves from future modification in that corresponding action we call it manually
        EditorHandler.Mapper.ModifyMapping(nameof(IEditor.IsSpellCheckEnabled), (handler, editor, _) =>
        {
            ((InputView)editor).IsSpellCheckEnabled = false;
            ((InputView)editor).IsTextPredictionEnabled = false;
            handler.PlatformView?.UpdateIsSpellCheckEnabled(editor);
        });

        EditorHandler.Mapper.ModifyMapping(nameof(IEditor.IsTextPredictionEnabled), (handler, editor, _) =>
        {
            ((InputView)editor).IsSpellCheckEnabled = false;
            ((InputView)editor).IsTextPredictionEnabled = false;
            handler.PlatformView?.UpdateIsTextPredictionEnabled(editor);
        });

        EntryHandler.Mapper.ModifyMapping(nameof(IEntry.IsSpellCheckEnabled), (handler, editor, _) =>
        {
            ((InputView)editor).IsSpellCheckEnabled = false;
            ((InputView)editor).IsTextPredictionEnabled = false;
            handler.PlatformView?.UpdateIsSpellCheckEnabled(editor);
        });

        EntryHandler.Mapper.ModifyMapping(nameof(IEntry.IsTextPredictionEnabled), (handler, editor, _) =>
        {
            ((InputView)editor).IsSpellCheckEnabled = false;
            ((InputView)editor).IsTextPredictionEnabled = false;
            handler.PlatformView?.UpdateIsTextPredictionEnabled(editor);
        });

        SearchBarHandler.Mapper.ModifyMapping(nameof(ISearchBar.IsSpellCheckEnabled), (handler, editor, _) =>
        {
            ((InputView)editor).IsSpellCheckEnabled = false;
            ((InputView)editor).IsTextPredictionEnabled = false;
            handler.PlatformView?.UpdateIsSpellCheckEnabled(editor);
        });

        SearchBarHandler.Mapper.ModifyMapping(nameof(ISearchBar.IsTextPredictionEnabled), (handler, editor, _) =>
        {
            ((InputView)editor).IsSpellCheckEnabled = false;
            ((InputView)editor).IsTextPredictionEnabled = false;
            handler.PlatformView?.UpdateIsTextPredictionEnabled(editor);
        });
    }
}
