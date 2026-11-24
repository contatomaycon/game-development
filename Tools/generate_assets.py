#!/usr/bin/env python3
"""Regenerate the original pixel art and audio for Farol de Íon.

Requires Pillow and NumPy. All shapes, motion poses, waveforms and note
sequences are defined below. No external images, samples or melodies are used.
"""

from pathlib import Path
import wave

import numpy as np
from PIL import Image, ImageDraw


RESOURCES = Path(__file__).resolve().parents[1] / "Assets" / "Resources"
SPRITES = RESOURCES / "Sprites"
AUDIO = RESOURCES / "Audio"
RATE = 44100
TAU = 2 * np.pi

INK = "#0e182c"
SHADOW = "#192b45"
SLATE = "#253f58"
METAL = "#3e6175"
PALE_METAL = "#6594a2"
TEAL = "#2c8f91"
AQUA = "#54dbc5"
ICE = "#aaf2de"
WHITE = "#e2fff2"
AMBER = "#ffcd72"
GOLD = "#e39648"
CORAL = "#fb816d"
RED = "#aa4c5f"


def canvas(size=(32, 32)):
    image = Image.new("RGBA", size)
    return image, ImageDraw.Draw(image)


def save_sprite(path, image):
    target = SPRITES / f"{path}.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    image.save(target, optimize=True)


def outlined_limb(draw, points, color, foot=False):
    draw.line(points, fill=INK, width=5)
    draw.line(points, fill=color, width=3)
    if foot:
        x, y = points[-1]
        draw.rectangle((x - 2, y - 1, x + 3, y + 1), fill=INK)
        draw.line((x - 1, y - 1, x + 2, y - 1), fill=PALE_METAL)


# Offsets to distal joints: far knee/foot and near knee/foot. Three frames in
# each stride use lifted or compressed legs so ground contact reads clearly.
WALK = [
    ((-3, 5), (-5, 10), (3, 5), (6, 10)),
    ((-3, 5), (-4, 9), (2, 6), (4, 10)),
    ((0, 6), (-1, 10), (1, 5), (1, 10)),
    ((2, 5), (4, 9), (-2, 6), (-3, 10)),
    ((3, 5), (6, 10), (-3, 5), (-5, 10)),
    ((2, 6), (4, 10), (-3, 5), (-4, 9)),
    ((1, 5), (1, 10), (0, 6), (-1, 10)),
    ((-2, 6), (-3, 10), (2, 5), (4, 9)),
]
RUN = [
    ((-4, 4), (-8, 9), (5, 4), (8, 10)),
    ((-5, 5), (-8, 7), (3, 6), (5, 10)),
    ((-4, 5), (-6, 3), (0, 6), (0, 10)),
    ((0, 4), (-3, 7), (-4, 6), (-6, 9)),
    ((5, 4), (8, 10), (-4, 4), (-8, 9)),
    ((3, 6), (5, 10), (-5, 5), (-8, 7)),
    ((0, 6), (0, 10), (-4, 5), (-6, 3)),
    ((-4, 6), (-6, 9), (0, 4), (-3, 7)),
]
JUMP = [
    ((-4, 4), (-5, 8), (4, 4), (5, 9)),
    ((-4, 4), (-6, 7), (4, 3), (3, 7)),
    ((-4, 5), (-6, 9), (5, 4), (6, 8)),
    ((-3, 5), (-4, 10), (4, 5), (5, 10)),
]
CLIMB = [
    ((1, 4), (0, 7), (2, 5), (1, 10)),
    ((0, 4), (-1, 6), (2, 6), (1, 9)),
    ((-1, 5), (-2, 8), (3, 5), (2, 8)),
    ((-1, 5), (-2, 10), (3, 4), (2, 7)),
    ((-1, 6), (-2, 9), (3, 4), (2, 6)),
    ((0, 5), (-1, 8), (3, 5), (2, 8)),
]


def player_pose(state, frame):
    image, draw = canvas()
    walking = state in ("Walk", "Run")
    leaning = int(state == "Run") - int(state == "Hurt")
    bob = int(walking and frame % 4 == 1) + int(state == "Idle" and frame == 2)
    y0 = bob
    center = 15 + leaning

    def p(x, y):
        return (x + center - 15, y + y0)

    def rect(box, fill):
        draw.rectangle((p(*box[:2]), p(*box[2:])), fill=fill)

    def polygon(points, fill):
        draw.polygon([p(*point) for point in points], fill=fill)

    far_leg = 12
    near_leg = 18
    stride = WALK[frame] if state == "Walk" else RUN[frame] if state == "Run" else None
    if state == "Jump":
        stride = JUMP[frame]
    if stride is None:
        stride = (-1, 5), (-2, 10), (1, 5), (2, 10)
    if state == "Climb":
        stride = CLIMB[frame]
    if state == "Hurt":
        stride = (
            ((-4, 4), (-5, 9), (3, 5), (4, 10)),
            ((-3, 4), (-4, 9), (4, 6), (5, 10)),
            ((-2, 5), (-4, 10), (2, 5), (3, 10)),
        )[frame]

    def leg(root, knee, foot, color):
        x, y = p(root, 19)
        outlined_limb(draw, ((x, y), (x + knee[0], y + knee[1]),
                             (x + foot[0], min(29, y + foot[1]))), color, foot=True)

    leg(far_leg, stride[0], stride[1], METAL)

    # Backpack, rounded chest shell and maintenance panel.
    polygon([(9, 11), (13, 11), (14, 13), (14, 21), (9, 21), (6, 19), (6, 14)], INK)
    rect((7, 14, 10, 19), TEAL)
    rect((7, 14, 7, 17), AQUA)
    rect((8, 19, 11, 20), SLATE)
    polygon([(11, 12), (19, 12), (22, 15), (22, 20), (20, 23),
             (11, 23), (9, 20), (9, 15)], INK)
    polygon([(12, 13), (19, 13), (21, 15), (21, 19), (19, 22),
             (12, 22), (10, 19), (10, 15)], TEAL)
    rect((11, 15, 14, 18), AQUA)
    rect((14, 15, 18, 19), ICE)
    rect((15, 16, 17, 18), PALE_METAL)
    rect((16, 16, 16, 17), AMBER)
    rect((12, 20, 19, 21), SLATE)
    leg(near_leg, stride[2], stride[3], PALE_METAL)

    # Glove poses deliberately favor the side silhouette.
    if state == "Climb":
        arm = [(23, 10), (23, 12), (24, 15), (24, 16), (24, 13), (23, 10)][frame]
    elif state == "Jump":
        arm = [(22, 12), (25, 12), (24, 16), (22, 18)][frame]
    elif walking:
        arm = (19 + [2, 1, 0, -2, -3, -1, 1, 3][frame], 19)
    elif state == "Hurt":
        arm = [(24, 13), (23, 15), (22, 19)][frame]
    else:
        arm = (21, 21)
    outlined_limb(draw, (p(18, 15), p(*arm)), AQUA)
    ax, ay = arm
    rect((ax - 1, ay - 1, ax + 1, ay), PALE_METAL)
    rect((ax - 1, ay, ax + 1, ay + 1), ICE)

    # Helmet, visor and small right-facing optical assembly.
    polygon([(11, 4), (21, 4), (25, 7), (25, 12), (22, 15),
             (11, 15), (8, 12), (8, 7)], INK)
    polygon([(11, 5), (21, 5), (23, 7), (24, 10), (22, 13),
             (11, 13), (9, 11), (9, 8)], ICE)
    rect((12, 5, 20, 5), WHITE)
    rect((10, 10, 12, 12), TEAL)
    rect((10, 8, 10, 9), AQUA)
    polygon([(15, 7), (23, 7), (24, 8), (24, 11), (22, 12),
             (15, 12), (13, 10), (13, 8)], SHADOW)
    rect((15, 8, 22, 8), METAL)
    if state == "Hurt":
        rect((18, 9, 21, 10), CORAL)
    elif state == "Idle" and frame == 3:
        rect((20, 10, 22, 10), AMBER)
    else:
        rect((20, 8, 22, 10), AMBER)
        rect((21, 8, 22, 8), WHITE)
    draw.line((p(11, 5), p(10, 2)), fill=SLATE, width=2)
    rect((9, 1, 11, 2), AMBER if frame % 3 else GOLD)
    return image


def patrol_pose(frame):
    image, draw = canvas()
    bob = [0, -1, -2, -2, -1, 0][frame]
    flame = [3, 5, 7, 4, 6, 4][frame]

    def rect(box, color):
        x1, y1, x2, y2 = box
        draw.rectangle((x1, y1 + bob, x2, y2 + bob), fill=color)

    def polygon(points, color):
        draw.polygon([(x, y + bob) for x, y in points], fill=color)

    for x in (10, 21):
        polygon([(x - 2, 21), (x + 2, 21), (x, 21 + flame)], RED)
        polygon([(x - 1, 21), (x + 1, 21), (x, 20 + flame)], CORAL)
        draw.point((x, 22 + bob), fill=AMBER)

    polygon([(6, 8), (23, 8), (28, 12), (28, 18), (24, 23),
             (8, 23), (3, 19), (3, 12)], INK)
    polygon([(7, 9), (23, 9), (27, 13), (27, 18), (23, 21),
             (8, 21), (4, 18), (4, 13)], METAL)
    polygon([(7, 10), (21, 10), (23, 12), (8, 12), (5, 14), (5, 12)], PALE_METAL)
    polygon([(8, 17), (24, 17), (27, 15), (26, 19), (23, 21),
             (8, 21), (5, 19)], SLATE)
    rect((10, 6, 19, 9), INK)
    rect((12, 6, 17, 8), RED)
    if frame in (1, 2, 3):
        rect((14, 6, 16, 7), CORAL)
    polygon([(15, 12), (25, 12), (26, 13), (26, 17), (24, 18),
             (15, 18), (13, 16), (13, 14)], SHADOW)
    rect((18, 14, 24, 15), RED)
    rect((21, 14, 25, 15), CORAL)
    rect((23, 14, 24, 14), AMBER)
    rect((7, 15, 9, 16), TEAL)
    rect((7, 16, 8, 16), AQUA)
    rect((9, 20, 12, 22), INK)
    rect((20, 20, 23, 22), INK)
    return image


def platform():
    image, draw = canvas()
    draw.rectangle((0, 0, 31, 31), fill=SLATE)
    draw.rectangle((0, 0, 31, 4), fill=PALE_METAL)
    draw.line((0, 0, 31, 0), fill=ICE)
    draw.line((0, 5, 31, 5), fill=INK)
    draw.line((0, 7, 31, 7), fill=TEAL)
    draw.rectangle((2, 10, 29, 25), fill=SHADOW)
    draw.line((2, 10, 29, 10), fill=METAL)
    draw.line((2, 26, 29, 26), fill=METAL)
    for start in (5, 11, 17, 23):
        draw.polygon([(start, 12), (start + 2, 12), (start + 5, 22),
                      (start + 3, 22)], fill=SLATE)
    draw.rectangle((3, 28, 10, 29), fill=TEAL)
    draw.rectangle((4, 28, 7, 28), fill=AQUA)
    draw.rectangle((23, 28, 28, 28), fill=METAL)
    for x in (1, 30):
        draw.point((x, 3), fill=AMBER)
        draw.point((x, 26), fill=PALE_METAL)
    draw.line((0, 31, 31, 31), fill=INK)
    return image


def climb():
    image, draw = canvas()
    for x in (5, 24):
        draw.rectangle((x, 0, x + 3, 31), fill=INK)
        draw.line((x + 1, 0, x + 1, 31), fill=TEAL)
        draw.line((x + 2, 0, x + 2, 31), fill=AQUA)
    for y in (2, 10, 18, 26):
        draw.rectangle((6, y, 26, y + 3), fill=INK)
        draw.line((8, y + 1, 24, y + 1), fill=PALE_METAL)
        draw.line((8, y + 2, 24, y + 2), fill=METAL)
        draw.point((7, y + 1), fill=AMBER)
        draw.point((25, y + 1), fill=AMBER)
    return image


def core():
    image, draw = canvas()
    draw.polygon([(16, 2), (26, 8), (29, 15), (26, 23), (16, 29),
                  (6, 23), (3, 15), (6, 8)], fill=(70, 222, 191, 36))
    draw.polygon([(16, 5), (23, 10), (25, 16), (23, 21), (16, 26),
                  (9, 21), (7, 16), (9, 10)], fill=INK)
    draw.polygon([(16, 6), (23, 11), (23, 20), (16, 25), (9, 20),
                  (9, 11)], fill=TEAL)
    draw.polygon([(16, 7), (21, 13), (21, 18), (16, 24), (11, 18),
                  (11, 13)], fill=AQUA)
    draw.polygon([(16, 10), (19, 14), (19, 17), (16, 21),
                  (13, 17), (13, 14)], fill=ICE)
    draw.line((16, 11, 16, 18), fill=WHITE, width=2)
    draw.point((4, 8), fill=AMBER)
    draw.point((27, 11), fill=AMBER)
    draw.point((24, 25), fill=ICE)
    return image


def spikes():
    image, draw = canvas()
    for start in (0, 10, 20):
        draw.polygon([(start, 27), (start + 5, 5), (start + 11, 27)], fill=INK)
        draw.polygon([(start + 2, 27), (start + 5, 8),
                      (start + 9, 27)], fill=RED)
        draw.polygon([(start + 3, 23), (start + 5, 9),
                      (start + 6, 18)], fill=CORAL)
        draw.line((start + 5, 10, start + 5, 13), fill=AMBER)
    draw.rectangle((0, 26, 31, 31), fill=INK)
    draw.line((0, 27, 31, 27), fill=METAL)
    for x in (2, 12, 22):
        draw.line((x, 29, x + 4, 29), fill=CORAL)
    return image


def exit_gate():
    image, draw = canvas()
    draw.polygon([(4, 31), (4, 9), (10, 3), (21, 3), (27, 9),
                  (27, 31)], fill=INK)
    draw.polygon([(5, 30), (5, 10), (10, 4), (21, 4), (26, 10),
                  (26, 30)], fill=SLATE)
    draw.polygon([(8, 29), (8, 11), (12, 7), (19, 7), (23, 11),
                  (23, 29)], fill=TEAL)
    draw.polygon([(10, 28), (10, 12), (13, 9), (18, 9), (21, 12),
                  (21, 28)], fill=SHADOW)
    draw.line((10, 14, 10, 27), fill=AQUA)
    draw.line((21, 14, 21, 27), fill=AQUA)
    draw.rectangle((13, 15, 18, 21), fill=TEAL)
    draw.polygon([(14, 16), (17, 16), (19, 18), (17, 20), (14, 20),
                  (16, 18)], fill=AMBER)
    draw.rectangle((2, 29, 29, 31), fill=METAL)
    draw.line((9, 29, 22, 29), fill=ICE)
    draw.line((12, 5, 19, 5), fill=AMBER)
    return image


def checkpoint():
    image, draw = canvas()
    draw.rectangle((6, 26, 25, 31), fill=INK)
    draw.rectangle((8, 26, 23, 29), fill=METAL)
    draw.line((9, 26, 22, 26), fill=PALE_METAL)
    draw.rectangle((13, 12, 18, 26), fill=INK)
    draw.rectangle((14, 13, 17, 25), fill=TEAL)
    draw.line((14, 15, 14, 24), fill=AQUA)
    draw.polygon([(11, 13), (9, 9), (12, 4), (19, 4), (22, 9),
                  (20, 13)], fill=INK)
    draw.polygon([(11, 9), (13, 5), (18, 5), (20, 9), (18, 12),
                  (13, 12)], fill=GOLD)
    draw.rectangle((14, 6, 17, 9), fill=AMBER)
    draw.point((15, 6), fill=WHITE)
    draw.point((6, 6), fill=AMBER)
    draw.point((25, 6), fill=AMBER)
    return image


def orbital_backdrop():
    image, draw = canvas((320, 180))
    rng = np.random.default_rng(217)
    for y in range(180):
        shade = (8 + y // 41, 15 + y // 20, 31 + y // 10)
        draw.line((0, y, 319, y), fill=shade)
    for _ in range(132):
        x, y = rng.integers((0, 0), (320, 139))
        color = ("#253951", "#294159", "#35536a", "#435f76")[int(rng.integers(4))]
        draw.point((int(x), int(y)), fill=color)
    for x, y in ((29, 24), (55, 67), (108, 35), (155, 20), (293, 49), (211, 78)):
        draw.line((x - 2, y, x + 2, y), fill="#2c495d")
        draw.line((x, y - 2, x, y + 2), fill="#2c495d")
        draw.point((x, y), fill="#52748a")

    for lines in ([(124, 102), (165, 93), (209, 77), (266, 55), (314, 35)],
                  [(113, 108), (159, 98), (202, 85), (260, 62), (319, 39)]):
        draw.line(lines, fill="#1e384b", width=1)
    draw.ellipse((193, -16, 289, 80), fill="#152e47", outline="#36576d")
    draw.ellipse((200, -11, 289, 85), fill="#17344e")
    draw.ellipse((210, -6, 294, 88), fill="#1a3a51")
    draw.ellipse((220, 2, 294, 88), fill="#244559")
    mask = Image.new("1", image.size)
    ImageDraw.Draw(mask).ellipse((193, -16, 289, 80), fill=1)
    surface = Image.new("RGBA", image.size)
    surface_draw = ImageDraw.Draw(surface)
    surface_draw.ellipse((252, -7, 328, 110), fill=(12, 27, 47, 200))
    for lines in ([(183, 36), (205, 32), (227, 33), (244, 42), (276, 45), (299, 54)],
                  [(180, 46), (210, 42), (230, 43), (243, 52), (261, 56), (295, 62)],
                  [(189, 59), (220, 51), (233, 55), (248, 63), (273, 66), (294, 70)]):
        surface_draw.line(lines, fill="#234057", width=2)
    image.paste(surface, (0, 0), Image.fromarray(
        np.where(np.array(mask), np.array(surface.getchannel("A")), 0).astype(np.uint8)
    ))
    draw.arc((193, -16, 289, 80), 75, 261, fill="#3d6577")
    draw.line([(172, 92), (209, 85), (266, 64), (314, 42)], fill="#315469", width=1)

    draw.polygon([(0, 130), (17, 130), (29, 119), (40, 119), (50, 132),
                  (68, 132), (83, 143), (105, 142), (121, 151), (147, 151),
                  (182, 128), (205, 128), (224, 137), (250, 135), (268, 120),
                  (291, 120), (309, 130), (319, 130), (319, 179), (0, 179)], fill="#102239")
    draw.rectangle((0, 148, 319, 153), fill="#132a40")
    draw.line((0, 147, 319, 147), fill="#1e374b")
    for x, h in ((7, 33), (25, 19), (61, 24), (94, 11), (148, 29), (166, 17),
                 (189, 25), (229, 13), (255, 18), (282, 29), (311, 22)):
        draw.rectangle((x, 147 - h, x + 3, 179), fill="#142d44")
        draw.point((x + 1, 146 - h), fill="#53636b")
    for x in range(6, 320, 13):
        draw.line((x, 150, x + 3, 150), fill="#315269")
        if x % 3 == 0:
            draw.point((x, 158), fill="#495261")
    draw.rectangle((0, 171, 319, 179), fill="#0c182b")
    draw.line((0, 170, 319, 170), fill="#1d364a")
    return image


def make_art():
    states = {"Idle": 4, "Walk": 8, "Run": 8, "Jump": 4, "Climb": 6, "Hurt": 3}
    for state, frame_count in states.items():
        for frame in range(frame_count):
            save_sprite(f"Player/{state}_{frame:02}", player_pose(state, frame))
    for frame in range(6):
        save_sprite(f"Enemy/Patrol_{frame:02}", patrol_pose(frame))

    for name, sprite in {
        "Platform": platform(), "Climb": climb(), "Core": core(),
        "Spikes": spikes(), "Exit": exit_gate(), "Checkpoint": checkpoint(),
    }.items():
        save_sprite(name, sprite)
    save_sprite("Backdrop/Orbit", orbital_backdrop())


def ramp(length, seconds, attack, release):
    t = np.arange(length) / RATE
    return np.minimum(1, t / attack) * np.minimum(1, (seconds - t) / release)


def tone(seconds, initial, final=None, volume=1.0, harmonic=0.15,
         attack=0.015, release=0.08, decay=0.0):
    length = round(seconds * RATE)
    t = np.arange(length) / RATE
    final = initial if final is None else final
    phase = TAU * (initial * t + (final - initial) * t**2 / (2 * seconds))
    carrier = np.sin(phase) + harmonic * np.sin(2 * phase)
    return volume * carrier * ramp(length, seconds, attack, release) * np.exp(-decay * t)


def midi(number):
    return 440 * 2 ** ((number - 69) / 12)


def place(track, audio, at):
    """Mix with wraparound so release tails preserve the music loop boundary."""
    offset = round(at * RATE) % len(track)
    segment = min(len(track) - offset, len(audio))
    track[offset:offset + segment] += audio[:segment]
    if segment < len(audio):
        track[:len(audio) - segment] += audio[segment:]


def save_sound(name, audio, peak):
    audio = audio / np.max(np.abs(audio)) * peak
    pcm = np.round(audio * 32767).astype("<i2")
    with wave.open(str(AUDIO / f"{name}.wav"), "wb") as output:
        output.setnchannels(1 if audio.ndim == 1 else audio.shape[1])
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())


def arpeggio(notes, interval, duration, volume=1.0):
    seconds = (len(notes) - 1) * interval + duration + 0.03
    output = np.zeros(round(seconds * RATE))
    for index, note in enumerate(notes):
        place(output, tone(duration, midi(note), volume=volume, decay=4),
              index * interval)
    return output


def make_music():
    beat = 60 / 104
    bar_length = 4 * beat
    track = np.zeros((round(8 * bar_length * RATE), 2))
    chords = (
        (45, 52, 55, 60), (43, 50, 53, 57), (41, 48, 52, 55), (40, 47, 50, 55),
        (45, 52, 55, 62), (48, 55, 59, 64), (41, 48, 52, 57), (43, 50, 55, 60),
    )
    melody = (
        (72, 76, 79), (69, 74, 77), (67, 72, 76), (67, 71, 74),
        (72, 76, 81), (76, 79, 83), (69, 72, 76), (71, 74, 79),
    )
    for index, notes in enumerate(chords):
        start = index * bar_length
        for voice, note in enumerate(notes):
            t = tone(bar_length + 0.48, midi(note), volume=0.045 if voice else 0.065,
                     harmonic=0.08, attack=0.42, release=0.64)
            pulse = 0.90 + 0.10 * np.cos(TAU * np.arange(len(t)) / RATE / (beat * 2))
            pan = (1.0, 0.77) if voice % 2 == 0 else (0.76, 1.0)
            place(track, (t * pulse)[:, None] * pan, start - 0.17)
        for offset in (0, 2, 3.5):
            bass = tone(0.27, midi(notes[0] - 12), volume=0.075,
                        harmonic=0.25, attack=0.012, release=0.19)
            place(track, bass[:, None] * [0.94, 0.94], start + offset * beat)
        for j, note in enumerate(melody[index]):
            at = start + (0.75, 1.75, 3.15)[j] * beat
            bell = tone(0.69, midi(note), volume=0.057, harmonic=0.25,
                        attack=0.019, release=0.38, decay=3)
            place(track, bell[:, None] * [1.0, 0.72], at)
            place(track, bell[:, None] * [0.15, 0.31], at + 0.46 * beat)

    save_sound("Music", track, peak=0.58)


def make_audio():
    AUDIO.mkdir(parents=True, exist_ok=True)
    save_sound("Jump", tone(0.20, 265, 620, harmonic=0.19, attack=0.007), peak=0.58)
    save_sound("Collect", arpeggio((79, 84, 88), 0.045, 0.30), peak=0.66)
    save_sound("Hurt", tone(0.31, 254, 92, harmonic=0.54, attack=0.008,
                            release=0.19, decay=1.2), peak=0.66)
    save_sound("Enemy", tone(0.23, 520, 240, harmonic=0.37, attack=0.01,
                             release=0.13, decay=3), peak=0.54)
    save_sound("Checkpoint", arpeggio((64, 69, 76, 81), 0.075, 0.35), peak=0.63)
    save_sound("Clear", arpeggio((57, 64, 69, 76, 81), 0.105, 0.54), peak=0.68)
    make_music()


if __name__ == "__main__":
    make_art()
    make_audio()
