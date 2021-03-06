﻿#version 110

in vec3 position;
in vec4 color;
in vec2 texCoords;

uniform mat4 view;

varying vec4 Color;
varying vec2 TexCoords;

void main()
{
    gl_Position = view * vec4(position, 1.0);
    Color = color;
    TexCoords = texCoords;
} 
