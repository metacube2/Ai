#pragma once

#include <juce_core/juce_core.h>
#include <juce_events/juce_events.h>

#include <atomic>
#include <functional>
#include <memory>
#include <thread>
#include <utility>

/**
    Waechter fuer Hintergrundauftraege.

    Ein Auftrag beim Media-AI-Dienst kann Minuten dauern; das Lesen laesst sich
    nicht sauber abbrechen. Statt beim Schliessen des Plugins auf den Thread zu
    warten und damit die DAW anzuhalten, meldet sich der Auftraggeber ab: der
    Thread laeuft ins Leere, das Ergebnis wird verworfen.
*/
class AuftragsWaechter
{
public:
    void abmelden() noexcept   { lebt.store (false); }
    bool aktiv() const noexcept { return lebt.load(); }

private:
    std::atomic<bool> lebt { true };
};

/** Fuehrt `arbeit` auf einem eigenen Thread aus und liefert das Ergebnis auf
    dem Message-Thread ab, sofern der Waechter noch aktiv ist. */
template <typename Ergebnis>
void starteAuftrag (std::shared_ptr<AuftragsWaechter> waechter,
                    std::function<Ergebnis()> arbeit,
                    std::function<void (const Ergebnis&)> abschluss)
{
    std::thread ([waechter = std::move (waechter),
                  arbeit = std::move (arbeit),
                  abschluss = std::move (abschluss)]
    {
        auto ergebnis = std::make_shared<Ergebnis> (arbeit());

        juce::MessageManager::callAsync ([waechter, ergebnis, abschluss]
        {
            if (waechter->aktiv())
                abschluss (*ergebnis);
        });
    }).detach();
}
