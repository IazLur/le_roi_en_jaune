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
