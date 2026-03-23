import sys
import subprocess
import os
import io

try:
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow"])
    from PIL import Image, ImageDraw, ImageFilter

try:
    from rembg import remove
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "rembg", "onnxruntime"])
    from rembg import remove

def create_horizontal_stripes(output_path):
    img = Image.new('RGBA', (110, 110), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img, 'RGBA')
    # 3 stripes across center
    y_centers = [35, 55, 75]
    for y in y_centers:
        # Base white, semi-transparent
        draw.rounded_rectangle([(0, y-7), (110, y+7)], radius=5, fill=(255, 255, 255, 200))
        # Top gloss highlight to make it look "candy glossy"
        draw.rounded_rectangle([(10, y-5), (100, y-1)], radius=2, fill=(255, 255, 255, 240))
    img.save(output_path)

def create_vertical_stripes(output_path):
    img = Image.new('RGBA', (110, 110), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img, 'RGBA')
    # 3 stripes across center
    x_centers = [35, 55, 75]
    for x in x_centers:
        # Base white, semi-transparent
        draw.rounded_rectangle([(x-7, 0), (x+7, 110)], radius=5, fill=(255, 255, 255, 200))
        # Left gloss highlight
        draw.rounded_rectangle([(x-5, 10), (x-1, 100)], radius=2, fill=(255, 255, 255, 240))
    img.save(output_path)

def process_bomb(in_path, out_path):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(in_path, 'rb') as i:
        input_data = i.read()
    
    # remove background smoothly
    output_data = remove(input_data)
    img = Image.open(io.BytesIO(output_data)).convert("RGBA")
    
    # Crop to bounding box of content
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
        
    # Make it exactly square based on the max dimension
    max_dim = max(img.width, img.height)
    square_img = Image.new("RGBA", (max_dim, max_dim), (0, 0, 0, 0))
    square_img.paste(img, ((max_dim - img.width) // 2, (max_dim - img.height) // 2))
    
    # Resize exactly to 110x110
    final_img = square_img.resize((110, 110), Image.Resampling.LANCZOS)
    final_img.save(out_path, "PNG")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Need bomb_in_path")
        sys.exit(1)
        
    bomb_in_path = sys.argv[1]
    
    base_dir = r"c:\Project\Match3\Assets\Textures\Sprites"
    os.makedirs(base_dir, exist_ok=True)
    
    h_path = os.path.join(base_dir, "striped_h.png")
    v_path = os.path.join(base_dir, "striped_v.png")
    bomb_out_path = os.path.join(base_dir, "bomb_overlay.png")
    
    print("Generating stripes...")
    create_horizontal_stripes(h_path)
    create_vertical_stripes(v_path)
    
    print(f"Processing bomb from {bomb_in_path} to {bomb_out_path}...")
    process_bomb(bomb_in_path, bomb_out_path)
    
    print("All done!")
