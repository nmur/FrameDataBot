CREATE TABLE IF NOT EXISTS characters (
  id TEXT PRIMARY KEY,
  game TEXT NOT NULL,
  name TEXT NOT NULL,
  source_character_id INT NULL,
  display_order INT NOT NULL DEFAULT 0,
  aliases JSONB NOT NULL DEFAULT '[]'::jsonb,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE(game, name)
);

CREATE TABLE IF NOT EXISTS moves (
  id TEXT PRIMARY KEY,
  character_id TEXT NOT NULL REFERENCES characters(id),
  section TEXT NOT NULL,
  canonical_name TEXT NOT NULL,
  display_order INT NULL,
  source_move_id TEXT NULL,
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

ALTER TABLE characters
  ADD COLUMN IF NOT EXISTS source_character_id INT NULL;

ALTER TABLE characters
  ADD COLUMN IF NOT EXISTS display_order INT NOT NULL DEFAULT 0;

ALTER TABLE moves
  ADD COLUMN IF NOT EXISTS display_order INT NULL;

ALTER TABLE moves
  ADD COLUMN IF NOT EXISTS source_move_id TEXT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_characters_source_character_id
  ON characters(source_character_id)
  WHERE source_character_id IS NOT NULL;

ALTER TABLE moves
  DROP CONSTRAINT IF EXISTS moves_character_id_section_canonical_name_key;

CREATE TABLE IF NOT EXISTS ingestion_run_character_statuses (
  run_id TEXT NOT NULL REFERENCES ingestion_runs(id) ON DELETE CASCADE,
  character_id TEXT NOT NULL,
  source_character_id INT NOT NULL,
  status TEXT NOT NULL,
  moves_processed INT NOT NULL DEFAULT 0,
  error TEXT NULL,
  PRIMARY KEY(run_id, character_id)
);
