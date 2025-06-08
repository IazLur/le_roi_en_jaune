"""Generate pixel-art textures using Stable Diffusion."""

import os
from PIL import Image

try:
    import torch
    from diffusers import StableDiffusionPipeline
except ImportError:
    raise SystemExit("Please install torch and diffusers: pip install --user torch diffusers transformers")


def generate_with_prompt(pipe, prompt: str, path: str, size=(64, 64), *, transparent=False):
    """Generate an image at ``path`` using ``prompt`` if missing.

    The output is resized with ``Image.NEAREST`` to preserve crisp pixel-art
    edges. If ``transparent`` is ``True``, the function makes the background
    color transparent using the color of the top-left pixel as reference.
    """
    if os.path.exists(path):
        return

    image = pipe(prompt).images[0]
    image = image.resize(size, Image.NEAREST)
    image = image.convert("RGBA")

    if transparent:
        bg_color = image.getpixel((0, 0))[:3]
        pixels = [
            (*px[:3], 0) if px[:3] == bg_color else px
            for px in image.getdata()
        ]
        image.putdata(pixels)

    image.save(path)


def main():
    os.makedirs('TheatreGame/Content', exist_ok=True)
    device = 'cuda' if torch.cuda.is_available() else 'cpu'
    pipe = StableDiffusionPipeline.from_pretrained('runwayml/stable-diffusion-v1-5')
    pipe = pipe.to(device)

    generate_with_prompt(
        pipe,
        'top down wooden floor texture, pixel art style',
        'TheatreGame/Content/stage_floor_ai.png'
    )
    generate_with_prompt(
        pipe,
        'red theatrical curtain, pixel art texture',
        'TheatreGame/Content/curtain_ai.png'
    )
    generate_with_prompt(
        pipe,
        'campfire sprite, pixel art, transparent background',
        'TheatreGame/Content/campfire_ai.png',
        transparent=True
    )
    generate_with_prompt(
        pipe,
        'simple wooden pawn, pixel art, transparent background',
        'TheatreGame/Content/pawn_ai.png',
        transparent=True
    )


if __name__ == '__main__':
    main()
