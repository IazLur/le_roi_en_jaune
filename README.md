# le_roi_en_jaune

Prototype de jeu 3D isométrique utilisant MonoGame.

## Installation

1. Installer le SDK .NET 6.0.
2. Restaurer les dépendances du projet :
   ```bash
   dotnet restore
   ```
3. Installer la dépendance Python Pillow :
   ```bash
   pip install --user pillow
   ```
4. Générer les textures nécessaires :
   ```bash
   python3 generate_textures.py
   ```
5. (Optionnel) Générer des textures de meilleure qualité avec un modèle Stable Diffusion :
   ```bash
   pip install --user torch diffusers transformers
   python3 generate_ai_textures.py
   ```
6. Lancer le jeu :
   ```bash
   dotnet run --project TheatreGame
   ```

## Détails

- Fenêtre : 1280x720
- Caméra : perspective isométrique simple
- Les textures de la scène sont générées avec `generate_textures.py` ou, pour un rendu plus réaliste, avec `generate_ai_textures.py`.
