Import("env")
import json
import os
import time
import sys
import re

def read_env_file():
    env_vars = {}
    env_file = os.path.join(env.get("PROJECT_DIR"), ".env")
    
    print(f"*** Looking for .env file at: {env_file}")
    
    if os.path.exists(env_file):
        try:
            with open(env_file, 'r') as f:
                content = f.read()
                print(f"*** .env file contents: {len(content)} bytes")
                f.seek(0)
                for line in f:
                    line = line.strip()
                    if line and not line.startswith('#'):
                        if '=' in line:
                            key, value = line.split('=', 1)
                            value = value.strip('"\'')
                            env_vars[key.strip()] = value
                            print(f"*** Found env var: {key.strip()} = {value}")
        except IOError as e:
            print(f"*** ERROR: Could not read .env file: {e}")
    else:
        print("*** WARNING: .env file not found, using defaults")
    
    return env_vars

def replace_env_vars(text, env_vars):
    def replacer(match):
        var_name = match.group(1)
        default_value = f"default-{var_name.lower().replace('_', '-')}"
        value = env_vars.get(var_name) or os.getenv(var_name, default_value)
        return value
    
    return re.sub(r'\$\{([A-Z_]+)\}', replacer, text)

def generate_settings_file(*args, **kwargs):
    print(f"\n*** generate_settings_file called with args: {args}, kwargs: {kwargs}")
    
    max_retries = 3
    env_vars = {}
    for attempt in range(max_retries):
        env_vars = read_env_file()
        if env_vars:
            break
        if attempt < max_retries - 1:
            print(f"*** Retry {attempt + 1}/{max_retries - 1} in 0.5s...")
            time.sleep(0.5)
    
    example_file = os.path.join(env.get("PROJECT_DIR"), "settings.template.json")
    if not os.path.exists(example_file):
        print(f"*** ERROR: settings.template.json not found at: {example_file}")
        sys.exit(1)
    
    try:
        with open(example_file, 'r') as f:
            template_content = f.read()
            print(f"*** Read settings.template.json ({len(template_content)} bytes)")
    except IOError as e:
        print(f"*** ERROR reading settings.template.json: {e}")
        sys.exit(1)
    
    processed_content = replace_env_vars(template_content, env_vars)
    
    try:
        settings = json.loads(processed_content)
        print(f"*** Using values:")
        print(f"    analyzerId: {settings['analyzerId']}")
        print(f"    mqtt.server: {settings['mqtt']['server']}")
        print(f"    mqtt.user: {settings['mqtt']['user']}")
        print(f"    mqtt.password: {'*' * len(settings['mqtt']['password'])}")
        print(f"    sensor.intervalSeconds: {settings['sensor']['intervalSeconds']}s")
        print(f"    display.intervalSeconds: {settings['display']['intervalSeconds']}s")
        print(f"    lowPowerMode: {settings['lowPowerMode']}")
    except json.JSONDecodeError as e:
        print(f"*** ERROR parsing JSON after variable substitution: {e}")
        print(f"*** Processed content:\n{processed_content}")
        sys.exit(1)
    
    data_dir = os.path.join(env.get("PROJECT_DIR"), "data")
    if not os.path.exists(data_dir):
        print(f"*** Creating data directory: {data_dir}")
        try:
            os.makedirs(data_dir)
        except OSError as e:
            print(f"*** ERROR creating data dir: {e}")
            sys.exit(1)
    else:
        print(f"*** Data directory exists: {data_dir}")
    
    settings_path = os.path.join(data_dir, "settings.json")
    try:
        with open(settings_path, 'w') as f:
            json.dump(settings, f, indent=2)
        print(f"*** Successfully wrote settings.json to: {settings_path}")
        
        if os.path.exists(settings_path):
            size = os.path.getsize(settings_path)
            print(f"*** Verified: settings.json exists ({size} bytes)")
        else:
            print("*** ERROR: settings.json was not created!")
            sys.exit(1)
            
    except IOError as e:
        print(f"*** ERROR writing settings.json: {e}")
        sys.exit(1)
    
    print(f"*** Generated settings.json from template ***\n")

if not hasattr(env, '_settings_actions_added'):
    env.AddPreAction("buildfs", generate_settings_file)
    env.AddPreAction("uploadfs", generate_settings_file)
    env._settings_actions_added = True
    print("*** Pre-actions added for buildfs and uploadfs")
else:
    print("*** Pre-actions already added, skipping")

print("*** Running generate_settings_file immediately ***")
generate_settings_file()