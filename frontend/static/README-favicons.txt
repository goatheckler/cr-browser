Favicons source:
- favicon.svg (authoritative)
To (re)generate PNGs from SVG (requires rsvg-convert or ImageMagick):

rsvg-convert -w 16 -h 16 favicon.svg > favicon-16.png
rsvg-convert -w 32 -h 32 favicon.svg > favicon-32.png

Add larger sizes as needed for PWA manifest:
rsvg-convert -w 192 -h 192 favicon.svg > icon-192.png
rsvg-convert -w 512 -h 512 favicon.svg > icon-512.png
