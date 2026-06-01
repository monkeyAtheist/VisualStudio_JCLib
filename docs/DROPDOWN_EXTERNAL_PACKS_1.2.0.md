# JC Lib Visual Studio 1.2.0 — listes déroulantes et pack embarqué optionnel

Cette version ajoute deux couleurs indépendantes : `DropdownBackground` et `DropdownText`. Elles pilotent les `ComboBox` et leurs `ComboBoxItem` sans imposer une modification globale du thème.

Le pack embarqué devient un fallback optionnel. La préférence `IncludeBundledPack` est désactivée par défaut. La Tool Window peut donc fonctionner exclusivement avec des packs globaux ou liés à une solution, notamment les packs produits depuis l’extension VS Code. Le JSON embarqué reste distribué comme filet de sécurité et peut être réactivé à tout moment.
