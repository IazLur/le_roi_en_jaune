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
4. Générer les textures nécessaires (les textures et la police seront créées/telechargées) :
   ```bash
   python3 generate_textures.py
   ```
5. (Optionnel) Générer des textures en pixel art avec Stable Diffusion :
   ```bash
   pip install --user torch diffusers transformers
   python3 generate_ai_textures.py
   ```
6. Le jeu charge la police `DejaVuSans.ttf` via la librairie FontStashSharp, aucun fichier `.xnb` n'est nécessaire.
7. Lancer le jeu :
   ```bash
   dotnet run --project TheatreGame
   ```

## Détails

- Fenêtre : 1280x720
- Caméra : perspective isométrique simple
- Les textures de la scène sont générées avec `generate_textures.py` ou, pour un rendu pixel art via l'IA, avec `generate_ai_textures.py`.
- Si une texture est présente dans `TheatreGame/ContentFinal`, elle est utilisée en priorité par rapport à celle de `TheatreGame/Content`.
