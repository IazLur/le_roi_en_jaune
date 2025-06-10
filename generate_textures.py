from PIL import Image, ImageDraw
import os
import urllib.request

os.makedirs('TheatreGame/Content', exist_ok=True)

# Download DejaVuSans.ttf if missing so the game can load a font at runtime
font_path = 'TheatreGame/Content/DejaVuSans.ttf'
if not os.path.exists(font_path):
    url = 'https://github.com/dejavu-fonts/dejavu-fonts/raw/master/ttf/DejaVuSans.ttf'
    print('Downloading', url)
    urllib.request.urlretrieve(url, font_path)


def save_if_missing(img: Image.Image, path: str):
    """Save ``img`` to ``path`` only if the file does not already exist."""
    if not os.path.exists(path):
        img.save(path)


# Stage floor: wooden planks
width, height = 128, 128
floor = Image.new('RGB', (width, height), (150, 100, 50))
draw = ImageDraw.Draw(floor)
for y in range(0, height, 16):
    draw.rectangle([0, y, width, y+1], fill=(130, 80, 40))
save_if_missing(floor, 'TheatreGame/Content/stage_floor.png')

# Simple red curtain texture
curtain = Image.new('RGB', (width, height), (160, 20, 40))
curtain_draw = ImageDraw.Draw(curtain)
for x in range(0, width, 8):
    curtain_draw.rectangle([x, 0, x+4, height], fill=(140, 10, 30))
save_if_missing(curtain, 'TheatreGame/Content/curtain.png')

# Transparent checkerboard overlay to delimit the board
grid = Image.new('RGBA', (width, height), (0, 0, 0, 0))
grid_draw = ImageDraw.Draw(grid)
square = 16
for y in range(0, height, square):
    for x in range(0, width, square):
        if ((x // square) + (y // square)) % 2 == 0:
            color = (0, 0, 0, 30)
        else:
            color = (0, 0, 0, 15)
        grid_draw.rectangle([x, y, x + square, y + square], fill=color)
save_if_missing(grid, 'TheatreGame/Content/grid_overlay.png')


# Very simple campfire sprite
fire = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
fire_draw = ImageDraw.Draw(fire)
# logs
fire_draw.rectangle([44, 88, 84, 100], fill=(110, 60, 30))
fire_draw.rectangle([52, 100, 76, 108], fill=(110, 60, 30))
# flames
fire_draw.polygon([(64, 40), (40, 88), (88, 88)], fill=(255, 160, 0))
fire_draw.polygon([(64, 56), (52, 88), (76, 88)], fill=(255, 220, 0))
save_if_missing(fire, 'TheatreGame/Content/campfire.png')

# Radial light gradient used for the flickering light
gradient = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
grad_draw = ImageDraw.Draw(gradient)
center = (128, 128)
for r in range(128, 0, -1):
    alpha = int(255 * ((r / 128) ** 2))
    grad_draw.ellipse([center[0]-r, center[1]-r, center[0]+r, center[1]+r],
                     fill=(255, 200, 50, alpha))

# Simple pawn piece texture used for characters
pawn = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
pawn_draw = ImageDraw.Draw(pawn)
# base
pawn_draw.ellipse([32, 96, 96, 120], fill=(0, 0, 0))
# body
pawn_draw.rectangle([56, 56, 72, 96], fill=(0, 0, 0))
pawn_draw.ellipse([48, 36, 80, 68], fill=(0, 0, 0))
# head
pawn_draw.ellipse([52, 24, 76, 48], fill=(0, 0, 0))
pawn.save('TheatreGame/Content/pawn.png')
save_if_missing(gradient, 'TheatreGame/Content/light_gradient.png')

# End turn button
button_w, button_h = 140, 40
button = Image.new('RGBA', (button_w, button_h), (80, 80, 80, 255))
btn_draw = ImageDraw.Draw(button)
btn_draw.rectangle([0, 0, button_w-1, button_h-1], outline=(200, 200, 200))
try:
    from PIL import ImageFont
    font = ImageFont.load_default()
    text = "End turn"
    text_w, text_h = btn_draw.textsize(text, font=font)
    btn_draw.text(((button_w - text_w) / 2, (button_h - text_h) / 2),
                  text, font=font, fill=(255, 255, 255))
except Exception:
    pass
save_if_missing(button, 'TheatreGame/Content/end_turn.png')

# Simple bishop-like piece texture
bishop = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
bishop_draw = ImageDraw.Draw(bishop)
bishop_draw.ellipse([32, 96, 96, 120], fill=(0, 0, 0))
bishop_draw.polygon([(64, 24), (48, 80), (80, 80)], fill=(0, 0, 0))
bishop_draw.ellipse([56, 20, 72, 36], fill=(0, 0, 0))
save_if_missing(bishop, 'TheatreGame/Content/bishop.png')

# Loading spinner texture (ring with a missing quarter)
spinner_size = 64
spinner = Image.new('RGBA', (spinner_size, spinner_size), (0, 0, 0, 0))
spinner_draw = ImageDraw.Draw(spinner)
outer_r = spinner_size // 2
inner_r = spinner_size // 2 - 6
spinner_draw.ellipse([
    spinner_size/2 - outer_r, spinner_size/2 - outer_r,
    spinner_size/2 + outer_r, spinner_size/2 + outer_r
], outline=(200, 200, 200, 255), width=6)
spinner_draw.pieslice([
    spinner_size/2 - outer_r, spinner_size/2 - outer_r,
    spinner_size/2 + outer_r, spinner_size/2 + outer_r
], 0, 90, fill=(0, 0, 0, 0))
save_if_missing(spinner, 'TheatreGame/Content/spinner.png')

# Grey circle texture used for smoke particles
smoke_size = 64
smoke = Image.new('RGBA', (smoke_size, smoke_size), (0, 0, 0, 0))
smoke_draw = ImageDraw.Draw(smoke)
smoke_draw.ellipse([0, 0, smoke_size - 1, smoke_size - 1], fill=(128, 128, 128, 100))
save_if_missing(smoke, 'TheatreGame/Content/smoke_particle.png')

# Simple fog texture used for fog of war overlay
fog = Image.new('RGBA', (128, 128), (100, 100, 100, 200))
save_if_missing(fog, 'TheatreGame/Content/fog.png')

# Simple apple sprite (32x32)
apple = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
apple_draw = ImageDraw.Draw(apple)
# apple body
apple_draw.ellipse([4, 6, 28, 30], fill=(200, 0, 0))
# stem
apple_draw.rectangle([14, 2, 18, 10], fill=(100, 60, 0))
save_if_missing(apple, 'TheatreGame/Content/apple.png')

# Basic oval shadow used for all entities
shadow = Image.new('RGBA', (64, 32), (0, 0, 0, 0))
shadow_draw = ImageDraw.Draw(shadow)
for r in range(16, 0, -1):
    alpha = int(150 * (r / 16))
    shadow_draw.ellipse([32 - r*2, 16 - r, 32 + r*2, 16 + r], fill=(0, 0, 0, alpha))
save_if_missing(shadow, 'TheatreGame/Content/shadow.png')
