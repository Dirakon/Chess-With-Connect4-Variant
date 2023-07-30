#!/bin/bash

INITIAL_DIR=$(pwd)
RAND_VAL=$RANDOM
mkdir "/tmp/tmp_webrtc_plugin_folder{$RAND_VAL}"                         
cd "/tmp/tmp_webrtc_plugin_folder{$RAND_VAL}"                             && \
wget "https://github.com/godotengine/webrtc-native/releases/download/1.0.1-stable/godot-extension-4.1-webrtc.zip" && \
unzip "godot-extension-4.1-webrtc.zip" -d  "$INITIAL_DIR"                               && \
rm "godot-extension-4.1-webrtc.zip"                                  && \
ls