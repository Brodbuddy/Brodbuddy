Import("env")
import json
import os

def read_env_file():
    """Read environment variables from .env file"""
    env_vars = {}
    env_file = os.path.join(env.get("PROJECT_DIR"), ".env")
    
    if os.path.exists(env_file):
        try:
            with open(env_file, 'r') as f:
                for line in f:
                    line = line.strip()
                    if line and not line.startswith('#'):
                        if '=' in line:
                            key, value = line.split('=', 1)
                            value = value.strip('"\'')
                            env_vars[key.strip()] = value
        except IOError:
            print("Warning: Could not read .env file")
    else:
        print("Warning: .env file not found, using defaults")
    
    return env_vars

def generate_settings_file(*args, **kwargs):
    env_vars = read_env_file()
    
    analyzer_id = env_vars.get('ANALYZER_ID') or os.getenv('ANALYZER_ID', 'default-analyzer-id')
    mqtt_server = env_vars.get('MQTT_SERVER') or os.getenv('MQTT_SERVER', 'mqtt.brodbuddy.com')
    mqtt_user = env_vars.get('MQTT_USER') or os.getenv('MQTT_USER', 'user')
    mqtt_password = env_vars.get('MQTT_PASSWORD') or os.getenv('MQTT_PASSWORD', 'pass')
    
    settings = {
        "analyzerId": analyzer_id,
        "mqtt": {
            "server": mqtt_server,
            "port": 1883,
            "user": mqtt_user,
            "password": mqtt_password
        },
        "sensor": {
            "interval": 15
        },
        "display": {
            "interval": 300
        },
        "calibration": {
            "tempOffset": -1.7,
            "humOffset": 7.3,
            "containerHeight": 200
        },
        "lowPowerMode": False
    }
    
    data_dir = os.path.join(env.get("PROJECT_DIR"), "data")
    if not os.path.exists(data_dir):
        os.makedirs(data_dir)
    
    settings_path = os.path.join(data_dir, "settings.json")
    with open(settings_path, 'w') as f:
        json.dump(settings, f, indent=2)
    
    print(f"*** Generated settings.json with analyzer ID: {analyzer_id} ***")

env.AddPreAction("buildfs", generate_settings_file)
env.AddPreAction("uploadfs", generate_settings_file)

print("*** Running generate_settings_file immediately ***")
generate_settings_file()