import pygame  #Z1
from pygame.locals import *  #Z2
from OpenGL.GL import *  #Z3
from OpenGL.GLU import *  #Z4
import numpy as np  #Z5
import math  #Z6
from datetime import datetime  #Z7
  #Z8
# Initialisierung  #Z9
pygame.init()  #Z10
display = (800, 600)  #Z11
pygame.display.set_mode(display, DOUBLEBUF | OPENGL)  #Z12
pygame.display.set_caption("3D Rotierende Uhr")  #Z13
  #Z14
gluPerspective(45, (display[0] / display[1]), 0.1, 50.0)  #Z15
glTranslatef(0.0, 0.0, -10)  #Z16
  #Z17
def draw_digit_segment(x, y, z, width, height, depth):  #Z18
    """Zeichnet ein 3D-Segment fuer eine Ziffer."""  #Z19
    glBegin(GL_QUADS)  #Z20
    # Vorderseite  #Z21
    glVertex3f(x, y, z)  #Z22
    glVertex3f(x + width, y, z)  #Z23
    glVertex3f(x + width, y + height, z)  #Z24
    glVertex3f(x, y + height, z)  #Z25
    # Rueckseite  #Z26
    glVertex3f(x, y, z - depth)  #Z27
    glVertex3f(x + width, y, z - depth)  #Z28
    glVertex3f(x + width, y + height, z - depth)  #Z29
    glVertex3f(x, y + height, z - depth)  #Z30
    # Oberseite  #Z31
    glVertex3f(x, y + height, z)  #Z32
    glVertex3f(x + width, y + height, z)  #Z33
    glVertex3f(x + width, y + height, z - depth)  #Z34
    glVertex3f(x, y + height, z - depth)  #Z35
    # Unterseite  #Z36
    glVertex3f(x, y, z)  #Z37
    glVertex3f(x + width, y, z)  #Z38
    glVertex3f(x + width, y, z - depth)  #Z39
    glVertex3f(x, y, z - depth)  #Z40
    # Rechte Seite  #Z41
    glVertex3f(x + width, y, z)  #Z42
    glVertex3f(x + width, y, z - depth)  #Z43
    glVertex3f(x + width, y + height, z - depth)  #Z44
    glVertex3f(x + width, y + height, z)  #Z45
    # Linke Seite  #Z46
    glVertex3f(x, y, z)  #Z47
    glVertex3f(x, y, z - depth)  #Z48
    glVertex3f(x, y + height, z - depth)  #Z49
    glVertex3f(x, y + height, z)  #Z50
    glEnd()  #Z51
  #Z52
# 7-Segment-Display Definition  #Z53
SEGMENTS = {  #Z54
    0: [1, 1, 1, 1, 1, 1, 0],  #Z55
    1: [0, 1, 1, 0, 0, 0, 0],  #Z56
    2: [1, 1, 0, 1, 1, 0, 1],  #Z57
    3: [1, 1, 1, 1, 0, 0, 1],  #Z58
    4: [0, 1, 1, 0, 0, 1, 1],  #Z59
    5: [1, 0, 1, 1, 0, 1, 1],  #Z60
    6: [1, 0, 1, 1, 1, 1, 1],  #Z61
    7: [1, 1, 1, 0, 0, 0, 0],  #Z62
    8: [1, 1, 1, 1, 1, 1, 1],  #Z63
    9: [1, 1, 1, 1, 0, 1, 1],  #Z64
}  #Z65
def get_oscillating_color(base_r, base_g, base_b, offset=0):  #Z66
    """Berechnet oszillierende Farbe basierend auf Zeit."""  #Z67
    t = pygame.time.get_ticks() / 1000.0  # Zeit in Sekunden  #Z68
    # Verschiedene Frequenzen fuer interessante Farbmuster  #Z69
    r = base_r * (0.5 + 0.5 * math.sin(t * 2.0 + offset))  #Z70
    g = base_g * (0.5 + 0.5 * math.sin(t * 2.5 + offset + 2.094))  #Z71
    b = base_b * (0.5 + 0.5 * math.sin(t * 3.0 + offset + 4.188))  #Z72
    return (max(0.2, r), max(0.2, g), max(0.2, b))  #Z73
  #Z74
def draw_7segment_digit(digit, x_offset):  #Z75
    """Zeichnet eine 3D 7-Segment-Ziffer."""  #Z76
    seg = SEGMENTS[digit]  #Z77
    depth = 0.1  #Z78
    thickness = 0.15  #Z79
  #Z80
    # Segment-Positionen: oben, rechts-oben, rechts-unten, unten, links-unten, links-oben, mitte  #Z81
    if seg[0]:  # Oben  #Z82
        r, g, b = get_oscillating_color(0.2, 1.0, 0.8, x_offset * 0.5)  #Z83
        glColor3f(r, g, b)  #Z84
        draw_digit_segment(x_offset - 0.4, 0.8, depth, 0.8, thickness, depth * 2)  #Z85
    if seg[1]:  # Rechts oben  #Z86
        r, g, b = get_oscillating_color(0.3, 0.9, 1.0, x_offset * 0.5 + 0.5)  #Z87
        glColor3f(r, g, b)  #Z88
        draw_digit_segment(x_offset + 0.25, 0.1, depth, thickness, 0.7, depth * 2)  #Z89
    if seg[2]:  # Rechts unten  #Z90
        r, g, b = get_oscillating_color(0.5, 0.8, 1.0, x_offset * 0.5 + 1.0)  #Z91
        glColor3f(r, g, b)  #Z92
        draw_digit_segment(x_offset + 0.25, -0.7, depth, thickness, 0.7, depth * 2)  #Z93
    if seg[3]:  # Unten  #Z94
        r, g, b = get_oscillating_color(0.8, 0.5, 1.0, x_offset * 0.5 + 1.5)  #Z95
        glColor3f(r, g, b)  #Z96
        draw_digit_segment(x_offset - 0.4, -0.85, depth, 0.8, thickness, depth * 2)  #Z97
    if seg[4]:  # Links unten  #Z98
        r, g, b = get_oscillating_color(1.0, 0.4, 0.8, x_offset * 0.5 + 2.0)  #Z99
        glColor3f(r, g, b)  #Z100
        draw_digit_segment(x_offset - 0.55, -0.7, depth, thickness, 0.7, depth * 2)  #Z101
    if seg[5]:  # Links oben  #Z102
        r, g, b = get_oscillating_color(1.0, 0.6, 0.5, x_offset * 0.5 + 2.5)  #Z103
        glColor3f(r, g, b)  #Z104
        draw_digit_segment(x_offset - 0.55, 0.1, depth, thickness, 0.7, depth * 2)  #Z105
    if seg[6]:  # Mitte  #Z106
        r, g, b = get_oscillating_color(1.0, 1.0, 0.3, x_offset * 0.5 + 3.0)  #Z107
        glColor3f(r, g, b)  #Z108
        draw_digit_segment(x_offset - 0.4, -0.02, depth, 0.8, thickness, depth * 2)  #Z109
  #Z110
def draw_colon(x_offset):  #Z111
    """Zeichnet einen oszillierenden Doppelpunkt."""  #Z112
    t = pygame.time.get_ticks() / 1000.0  #Z113
    # Pulsierender Doppelpunkt  #Z114
    intensity = 0.5 + 0.5 * math.sin(t * 4.0)  #Z115
    r, g, b = get_oscillating_color(1.0, 1.0, 0.2, x_offset)  #Z116
    glColor3f(r * intensity + 0.3, g * intensity + 0.3, b * intensity)  #Z117
    draw_digit_segment(x_offset - 0.1, 0.3, 0.1, 0.2, 0.2, 0.2)  #Z118
    draw_digit_segment(x_offset - 0.1, -0.4, 0.1, 0.2, 0.2, 0.2)  #Z119
  #Z120
def draw_clock():  #Z121
    """Zeichnet die komplette 3D-Uhr."""  #Z122
    now = datetime.now()  #Z123
    hours = now.hour  #Z124
    minutes = now.minute  #Z125
    seconds = now.second  #Z126
  #Z127
    # Stunden  #Z128
    draw_7segment_digit(hours // 10, -4.0)  #Z129
    draw_7segment_digit(hours % 10, -2.5)  #Z130
  #Z131
    # Doppelpunkt  #Z132
    draw_colon(-1.3)  #Z133
  #Z134
    # Minuten  #Z135
    draw_7segment_digit(minutes // 10, -0.2)  #Z136
    draw_7segment_digit(minutes % 10, 1.3)  #Z137
  #Z138
    # Doppelpunkt  #Z139
    draw_colon(2.5)  #Z140
  #Z141
    # Sekunden  #Z142
    draw_7segment_digit(seconds // 10, 3.6)  #Z143
    draw_7segment_digit(seconds % 10, 5.1)  #Z144
  #Z145
def main():  #Z146
    """Hauptschleife."""  #Z147
    rotation_x = 15  # Anfangsneigung  #Z148
    rotation_y = 0  #Z149
    clock = pygame.time.Clock()  #Z150
    mouse_dragging = False  #Z151
    last_mouse_pos = (0, 0)  #Z152
  #Z153
    while True:  #Z154
        for event in pygame.event.get():  #Z155
            if event.type == pygame.QUIT:  #Z156
                pygame.quit()  #Z157
                return  #Z158
            if event.type == pygame.KEYDOWN:  #Z159
                if event.key == pygame.K_ESCAPE:  #Z160
                    pygame.quit()  #Z161
                    return  #Z162
            if event.type == pygame.MOUSEBUTTONDOWN:  #Z163
                if event.button == 1:  # Linke Maustaste  #Z164
                    mouse_dragging = True  #Z165
                    last_mouse_pos = pygame.mouse.get_pos()  #Z166
            if event.type == pygame.MOUSEBUTTONUP:  #Z167
                if event.button == 1:  #Z168
                    mouse_dragging = False  #Z169
            if event.type == pygame.MOUSEMOTION:  #Z170
                if mouse_dragging:  #Z171
                    current_pos = pygame.mouse.get_pos()  #Z172
                    dx = current_pos[0] - last_mouse_pos[0]  #Z173
                    dy = current_pos[1] - last_mouse_pos[1]  #Z174
                    rotation_y += dx * 0.5  # Horizontale Bewegung = Y-Rotation  #Z175
                    rotation_x += dy * 0.5  # Vertikale Bewegung = X-Rotation  #Z176
                    last_mouse_pos = current_pos  #Z177
  #Z178
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)  #Z179
        glEnable(GL_DEPTH_TEST)  #Z180
  #Z181
        glPushMatrix()  #Z182
        glRotatef(rotation_x, 1, 0, 0)  # X-Achsen-Rotation (Maus hoch/runter)  #Z183
        glRotatef(rotation_y, 0, 1, 0)  # Y-Achsen-Rotation (Maus links/rechts)  #Z184
  #Z185
        draw_clock()  #Z186
  #Z187
        glPopMatrix()  #Z188
  #Z189
        pygame.display.flip()  #Z190
        clock.tick(60)  #Z191
  #Z192
if __name__ == "__main__":  #Z193
    main()  #Z194
