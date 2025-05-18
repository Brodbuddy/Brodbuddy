#!/bin/sh

JS_FILES=$(find /usr/share/nginx/html -name "*.js" -type f)

for file in $JS_FILES; do
  if [ ! -z "$VITE_HTTP_URL" ]; then
    sed -i "s|__VITE_HTTP_URL__|$VITE_HTTP_URL|g" $file
  fi
  
  if [ ! -z "$VITE_WS_URL" ]; then
    sed -i "s|__VITE_WS_URL__|$VITE_WS_URL|g" $file
  fi
done

exec "$@"