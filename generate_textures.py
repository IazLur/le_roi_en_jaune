from PIL import Image, ImageDraw
import os

os.makedirs('TheatreGame/Content', exist_ok=True)

# Stage floor: wooden planks
width, height = 128, 128
floor = Image.new('RGB', (width, height), (150, 100, 50))
draw = ImageDraw.Draw(floor)
for y in range(0, height, 16):
    draw.rectangle([0, y, width, y+1], fill=(130, 80, 40))
floor.save('TheatreGame/Content/stage_floor.png')

# Simple red curtain texture
curtain = Image.new('RGB', (width, height), (160, 20, 40))
curtain_draw = ImageDraw.Draw(curtain)
for x in range(0, width, 8):
    curtain_draw.rectangle([x, 0, x+4, height], fill=(140, 10, 30))
curtain.save('TheatreGame/Content/curtain.png')

# Transparent checkerboard overlay to delimit the board
grid = Image.new('RGBA', (width, height), (0, 0, 0, 0))
grid_draw = ImageDraw.Draw(grid)
square = 16
for y in range(0, height, square):
    for x in range(0, width, square):
        if ((x // square) + (y // square)) % 2 == 0:
            color = (200, 200, 200, 40)
        else:
            color = (120, 120, 120, 40)
        grid_draw.rectangle([x, y, x + square, y + square], fill=color)
grid.save('TheatreGame/Content/grid_overlay.png')


# Very simple campfire sprite
fire = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
fire_draw = ImageDraw.Draw(fire)
# logs
fire_draw.rectangle([44, 88, 84, 100], fill=(110, 60, 30))
fire_draw.rectangle([52, 100, 76, 108], fill=(110, 60, 30))
# flames
fire_draw.polygon([(64, 40), (40, 88), (88, 88)], fill=(255, 160, 0))
fire_draw.polygon([(64, 56), (52, 88), (76, 88)], fill=(255, 220, 0))
fire.save('TheatreGame/Content/campfire.png')

# Radial light gradient used for the flickering light
gradient = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
grad_draw = ImageDraw.Draw(gradient)
center = (64, 64)
for r in range(64, 0, -1):
    alpha = int(255 * (r / 64))
    grad_draw.ellipse([center[0]-r, center[1]-r, center[0]+r, center[1]+r],
                     fill=(255, 200, 50, alpha))
gradient.save('TheatreGame/Content/light_gradient.png')
