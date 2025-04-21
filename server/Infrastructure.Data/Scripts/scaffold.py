#!/usr/bin/env python3

# Dette er en kommentar i Python

# For at kunne køre det her script skal du:
# 1. Installere Python 3 (hvis du ikke har det, skriver python --version eller python3 --version i terminal)
# 2. Installere de nødvendige packages (psycopg2 og python-dotenv), skriv følgende i terminal: pip install psycopg2 python-dotenv
# 3. Kør scriptet: python scaffold.py (eller python3 scaffold.py)

# Det her er 'import statements', tilsvarende 'using' i toppen af en C# fil
import subprocess  # For at kunne køre shell kommandoer (tænk ligesom en konsol applikation kan modtage beskeder)
import psycopg2  # PostgreSQL klient, ligesom vi bruger Npgsql i C# for at forbinde til Postgres database
import os  # Ligesom System.IO i C#
from dotenv import load_dotenv  # Til at indlæse miljøvariabler fra .env filer

load_dotenv()  # Læser miljøvariabler fra .env fil, lidt ala appsettings.json

# Her har vi en variabel f.eks. DB_HOSt, som får værdien fra miljøvariablen APPOPTIONS__POSTGRES__HOST, hvis den ikke er der så er det bare "localhost"
DB_HOST = os.getenv("APPOPTIONS__POSTGRES__HOST", "localhost")
DB_PORT = os.getenv("APPOPTIONS__POSTGRES__PORT", "5432")
DB_NAME = os.getenv("APPOPTIONS__POSTGRES__DATABASE", "db")
DB_USER = os.getenv("APPOPTIONS__POSTGRES__USERNAME", "user")
DB_PASS = os.getenv("APPOPTIONS__POSTGRES__PASSWORD", "pass")

# Etablere forbindelse til vores database, ligesom NpgsqlConnection() i C#
connection = psycopg2.connect(
    host=DB_HOST, port=DB_PORT, user=DB_USER, password=DB_PASS, database=DB_NAME
)

cursor = connection.cursor()

# Eksekver SQL for at få alle tabel navne
cursor.execute("""
    SELECT table_name FROM information_schema.tables 
    WHERE table_schema = 'public'
""")
tables = cursor.fetchall()

# Her vælger vi hvilke tabeller (eller dem med bedste præfikse) vi IKKE vil scaffolde, i det her tilfælde vil vi gerne undgå flyway_ tabellen
excluded_prefixes = ["flyway_", "schema_", "__ef"]

table_list = []  # [] er en liste i Python
# Nu filtrere vi i vores tabeller, for vi egentlig går gennem alle tabel navnene i databasen og så tilføjer dem som ikke har de uønskede præfiks
for table in tables:
    table_name = table[0]
    if not any(table_name.startswith(prefix) for prefix in excluded_prefixes):
        table_list.append(table_name)

cursor.close()
connection.close()

connection_string = f"Host={DB_HOST};Port={DB_PORT};Database={DB_NAME};Username={DB_USER};Password={DB_PASS};"
table_params = " ".join(
    [f"--table {table}" for table in table_list]
)  # Her laver vi så en liste af tabeller vi vil scaffolde, hvilket så bliver alle tabeller undtagen dem vi har ekskluderet

# Her kører vi scaffold kommando som vi kender
scaffold_command = f"""dotnet ef dbcontext scaffold \
  "{connection_string}" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --output-dir ../Core/Entities \
  --context-dir Persistence/ \
  --context PgDbContext \
  --no-onconfiguring \
  --namespace Core.Entities \
  --context-namespace Infrastructure.Data.Persistence \
  --project Infrastructure.Data.csproj \
  --force \
  {table_params}"""

print(f"Running: {scaffold_command}")

subprocess.run(scaffold_command, shell=True)

print("Scaffolding complete!")
