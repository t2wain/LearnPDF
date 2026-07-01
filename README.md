# Extract Data from Engineering Drawings in PDF Format

The objective of this project is to create a library that can extract data from engineering drawings in PDF format. Primarily, the data includes the following:

- Equipment tags embedded throughout the drawing
- Information from the drawing title block:
	- Drawing number
	- Project number
	- Current revision
	- Revision history
	- Drawing titles

# Text Extraction in PDF file

For engineering drawing, text are placed throughout the drawing. The position of the text determines its meaning. Text are also placed together by blocks. Text within the same block provide meaning. 

Instrument tags are tyically placed on 2 separate lines together which form a block of text. The library can filter for these block of text that have exactly 2 lines to text to extract instrument tags.

Text are actually letters placed together to form words. The library provides simple method to extract horizontal text into words. For rotated text, a special algorithm is used to group letters into words.

Symbols within drawing have meaning. Text placed next to symbols or within symbols (like circcle and rectangle) have meaning. This library is able to detect the position of circles and rectangles so they can be associated the text placed near them.

# Analyze PDF file as an image

This libary can export a PDF drawing into a PNG image. The OpenCvSharp library can be used to detect complex symbols within an image.