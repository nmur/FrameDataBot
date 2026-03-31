CREATE TABLE IF NOT EXISTS characters (
  id TEXT PRIMARY KEY,
  game TEXT NOT NULL,
  name TEXT NOT NULL,
  aliases JSONB NOT NULL DEFAULT '[]'::jsonb,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE(game, name)
);

CREATE TABLE IF NOT EXISTS moves (
  id TEXT PRIMARY KEY,
  character_id TEXT NOT NULL REFERENCES characters(id),
  section TEXT NOT NULL,
  canonical_name TEXT NOT NULL,
  startup TEXT NULL,
  active TEXT NULL,
  recovery TEXT NULL,
  on_hit TEXT NULL,
  on_block TEXT NULL,
  frame_advantage TEXT NULL,
  notes TEXT NULL,
  UNIQUE(character_id, section, canonical_name)
);

CREATE TABLE IF NOT EXISTS ingestion_runs (
  id TEXT PRIMARY KEY,
  started_at TIMESTAMPTZ NOT NULL,
  completed_at TIMESTAMPTZ NULL,
  status TEXT NOT NULL,
  characters_processed INT NOT NULL DEFAULT 0,
  moves_processed INT NOT NULL DEFAULT 0,
  errors JSONB NOT NULL DEFAULT '[]'::jsonb
);
