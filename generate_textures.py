from PIL import Image, ImageDraw
import os

os.makedirs('TheatreGame/Content', exist_ok=True)

# Stage floor: wooden planks
width, height = 256, 256
floor = Image.new('RGB', (width, height), (150, 100, 50))
draw = ImageDraw.Draw(floor)
for y in range(0, height, 32):
    draw.rectangle([0, y, width, y+2], fill=(130, 80, 40))
floor.save('TheatreGame/Content/stage_floor.png')

# Simple red curtain texture
curtain = Image.new('RGB', (width, height), (160, 20, 40))
curtain_draw = ImageDraw.Draw(curtain)
for x in range(0, width, 16):
    curtain_draw.rectangle([x, 0, x+8, height], fill=(140, 10, 30))
curtain.save('TheatreGame/Content/curtain.png')

# Transparent checkerboard overlay to delimit the board
grid = Image.new('RGBA', (width, height), (0, 0, 0, 0))
grid_draw = ImageDraw.Draw(grid)
square = 32
for y in range(0, height, square):
    for x in range(0, width, square):
        if ((x // square) + (y // square)) % 2 == 0:
            color = (200, 200, 200, 80)
        else:
            color = (120, 120, 120, 80)
        grid_draw.rectangle([x, y, x + square, y + square], fill=color)
grid.save('TheatreGame/Content/grid_overlay.png')
