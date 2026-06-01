using System.Collections.ObjectModel;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class PrivacyViewModel : ViewModelBase
{
    public PrivacyViewModel(AppServices services, Func<OptimizationAction, ActionCardViewModel> cardFactory)
    {
        Actions =
        [
            cardFactory(new OptimizationAction { Id = "privacy.ad-id", Name = "Desactivar Advertising ID", Description = "Reduce identificación publicitaria por usuario.", Module = "Privacidad", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Si querés limitar personalización publicitaria local.", ExpectedImpact = "Cambia HKCU y crea backup reversible.", CurrentState = "Seguro", ExecuteAsync = services.Privacy.DisableAdvertisingIdAsync }),
            cardFactory(new OptimizationAction { Id = "privacy.start-suggestions", Name = "Sugerencias de Inicio", Description = "Oculta recomendaciones/promociones del menú Inicio.", Module = "Privacidad", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Para reducir ruido visual y sugerencias.", ExpectedImpact = "Cambia HKCU y puede requerir cerrar sesión para verse completo.", CurrentState = "Seguro", ExecuteAsync = services.Privacy.DisableStartSuggestionsAsync }),
            cardFactory(new OptimizationAction { Id = "privacy.tips", Name = "Tips de Windows", Description = "Desactiva consejos y mensajes sugeridos.", Module = "Privacidad", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Antes de streaming, capturas o uso diario sin interrupciones.", ExpectedImpact = "Menos notificaciones/sugerencias de Windows.", CurrentState = "Seguro", ExecuteAsync = services.Privacy.DisableTipsAsync }),
            cardFactory(new OptimizationAction { Id = "privacy.tailored", Name = "Experiencias personalizadas", Description = "Reduce personalización basada en diagnóstico.", Module = "Privacidad", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Si preferís menos recomendaciones basadas en diagnóstico.", ExpectedImpact = "Cambia preferencia de privacidad de usuario.", CurrentState = "Seguro", ExecuteAsync = services.Privacy.DisableTailoredExperiencesAsync }),
            cardFactory(new OptimizationAction { Id = "privacy.settings-recommendations", Name = "Recomendaciones Configuración", Description = "Oculta sugerencias de la app Configuración.", Module = "Privacidad", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Para una experiencia de Configuración más limpia.", ExpectedImpact = "Cambia HKCU y guarda backup.", CurrentState = "Seguro", ExecuteAsync = services.Privacy.DisableSettingsRecommendationsAsync }),
            cardFactory(new OptimizationAction { Id = "privacy.widgets", Name = "Widgets", Description = "Desactiva el botón de Widgets en la barra de tareas.", Module = "Privacidad", RiskLevel = RiskLevel.Medium, IsReversible = true, WhenToUse = "Si no usás Widgets y querés menos ruido en la barra.", ExpectedImpact = "Puede requerir reiniciar Explorer/cerrar sesión para reflejarse.", CurrentState = "Confirmación", IsAdvanced = true, ExecuteAsync = services.Privacy.DisableWidgetsAsync })
        ];
    }

    public ObservableCollection<ActionCardViewModel> Actions { get; }
}
