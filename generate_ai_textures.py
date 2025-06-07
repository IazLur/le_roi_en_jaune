import os
from PIL import Image

try:
    import torch
    from diffusers import StableDiffusionPipeline
except ImportError:
    raise SystemExit("Please install torch and diffusers: pip install --user torch diffusers transformers")


def generate_with_prompt(pipe, prompt: str, path: str, size=(512, 512)):
    """Generate an image at ``path`` using ``prompt`` if the file is missing."""
    if os.path.exists(path):
        return
    image = pipe(prompt).images[0]
    image = image.resize(size, Image.ANTIALIAS)
    image.save(path)


def main():
    os.makedirs('TheatreGame/Content', exist_ok=True)
    device = 'cuda' if torch.cuda.is_available() else 'cpu'
    pipe = StableDiffusionPipeline.from_pretrained('runwayml/stable-diffusion-v1-5')
    pipe = pipe.to(device)

    generate_with_prompt(pipe,
                        'top down view wooden floor texture for a small stage',
                        'TheatreGame/Content/stage_floor_hd.png')
    generate_with_prompt(pipe,
                        'theatrical red curtain texture, folds and shadows',
                        'TheatreGame/Content/curtain_hd.png')
    generate_with_prompt(pipe,
                        'glowing campfire with visible logs, isolated on transparent background',
                        'TheatreGame/Content/campfire_hd.png')
    generate_with_prompt(pipe,
                        'simple wooden pawn game piece on transparent background',
                        'TheatreGame/Content/pawn_hd.png')


if __name__ == '__main__':
    main()
