"""
Auricrux Open-Source TTS Service
Powered by Coqui TTS - an open-source, human-like text-to-speech engine
"""

from flask import Flask, request, send_file, jsonify
from flask_cors import CORS
import io
import logging
import os
from datetime import datetime

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(asctime)s] [%(levelname)s] %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)

# TTS Engine initialization
try:
    from TTS.api import TTS
    
    # Initialize TTS model (will download on first run)
    device = "cuda" if os.getenv("USE_GPU", "false").lower() == "true" else "cpu"
    model_name = os.getenv("TTS_MODEL", "tts_models/en/ljspeech/tacotron2-DDC")
    
    logger.info(f"Initializing TTS model: {model_name} on {device}")
    tts = TTS(model_name=model_name, gpu=(device == "cuda"), verbose=False)
    logger.info("✅ TTS model loaded successfully")
    
except ImportError:
    logger.error("❌ TTS module not found. Install with: pip install TTS")
    tts = None
except Exception as e:
    logger.error(f"❌ Failed to initialize TTS: {e}")
    tts = None


@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy' if tts else 'unavailable',
        'timestamp': datetime.utcnow().isoformat(),
        'model': os.getenv("TTS_MODEL", "tts_models/en/ljspeech/tacotron2-DDC")
    }), 200 if tts else 503


@app.route('/synthesize', methods=['POST'])
def synthesize():
    """
    Synthesize speech from text
    
    Request body:
    {
        "text": "What is the total cost of materials?",
        "voice": "en",  # language/voice code
        "format": "wav"  # wav, mp3, ogg
    }
    """
    try:
        if not tts:
            return jsonify({'error': 'TTS service not available'}), 503
        
        data = request.get_json()
        
        if not data or 'text' not in data:
            return jsonify({'error': 'No text provided'}), 400
        
        text = data['text'].strip()
        if not text:
            return jsonify({'error': 'Empty text'}), 400
        
        if len(text) > 5000:
            return jsonify({'error': 'Text too long (max 5000 characters)'}), 400
        
        voice = data.get('voice', 'en')
        audio_format = data.get('format', 'wav').lower()
        
        logger.info(f"Synthesizing: {len(text)} chars in {audio_format} format")
        
        # Synthesize speech
        wav_path = tts.tts_to_file(text=text, file_path="temp.wav")
        
        # Read WAV file
        with open(wav_path, 'rb') as f:
            audio_data = f.read()
        
        # Clean up
        try:
            os.remove(wav_path)
        except:
            pass
        
        # Convert if needed (simplified - normally use ffmpeg)
        if audio_format == 'wav':
            content_type = 'audio/wav'
        elif audio_format == 'mp3':
            content_type = 'audio/mpeg'
            # Note: mp3 conversion requires ffmpeg - for now return wav
            logger.warning(f"MP3 not yet supported, returning WAV")
        else:
            content_type = 'audio/wav'
        
        return send_file(
            io.BytesIO(audio_data),
            mimetype=content_type,
            as_attachment=False,
            download_name=f"audio.{audio_format}"
        )
        
    except Exception as e:
        logger.error(f"Synthesis error: {e}")
        return jsonify({'error': str(e)}), 500


@app.route('/voices', methods=['GET'])
def list_voices():
    """List available voices/languages"""
    voices = {
        'en': 'English (Default)',
        'de': 'German',
        'es': 'Spanish',
        'fr': 'French',
        'it': 'Italian',
        'pt': 'Portuguese',
        'nl': 'Dutch',
        'pl': 'Polish',
        'ru': 'Russian',
    }
    return jsonify({'voices': voices, 'default': 'en'}), 200


@app.route('/models', methods=['GET'])
def list_models():
    """List available TTS models"""
    models = [
        {
            'name': 'tts_models/en/ljspeech/tacotron2-DDC',
            'description': 'Tacotron2 (recommended, fast, good quality)',
            'language': 'English',
            'speed': 'Fast'
        },
        {
            'name': 'tts_models/en/ljspeech/glow-tts',
            'description': 'Glow-TTS (very fast, good quality)',
            'language': 'English',
            'speed': 'Very Fast'
        },
        {
            'name': 'tts_models/multilingual/multi-dataset/xtts_v2',
            'description': 'XTTS v2 (multilingual, high quality)',
            'language': 'Multilingual',
            'speed': 'Moderate'
        }
    ]
    return jsonify({'models': models, 'current': os.getenv("TTS_MODEL", "tts_models/en/ljspeech/tacotron2-DDC")}), 200


@app.route('/info', methods=['GET'])
def info():
    """Get server information"""
    return jsonify({
        'service': 'Auricrux TTS (Open-Source)',
        'version': '1.0.0',
        'engine': 'Coqui TTS',
        'timestamp': datetime.utcnow().isoformat(),
        'status': 'ready' if tts else 'not_initialized',
        'gpu_enabled': os.getenv("USE_GPU", "false").lower() == "true",
        'model': os.getenv("TTS_MODEL", "tts_models/en/ljspeech/tacotron2-DDC")
    }), 200


if __name__ == '__main__':
    port = int(os.getenv("PORT", 5000))
    debug = os.getenv("DEBUG", "false").lower() == "true"
    
    logger.info(f"🚀 Auricrux TTS Service starting on port {port}")
    logger.info(f"📡 Engine: Coqui TTS")
    logger.info(f"🔊 Model: {os.getenv('TTS_MODEL', 'tts_models/en/ljspeech/tacotron2-DDC')}")
    logger.info(f"💻 Device: {'GPU' if os.getenv('USE_GPU', 'false').lower() == 'true' else 'CPU'}")
    
    app.run(host='0.0.0.0', port=port, debug=debug)
