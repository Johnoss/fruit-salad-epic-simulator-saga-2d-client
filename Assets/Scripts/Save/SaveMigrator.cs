using UnityEngine;

namespace Save
{
    /// <summary>
    /// Brings an older payload up to the current schema.
    ///
    /// There is nothing to migrate yet - version 1 is the first shipped schema - but the mechanism
    /// exists from day one on purpose. Certification requires saves stay backward compatible across
    /// versions, and the moment a real migration is needed there are already saves in the wild that
    /// cannot be re-written.
    /// </summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData saveData)
        {
            if (saveData == null)
            {
                return new SaveData();
            }

            while (saveData.SchemaVersion < SaveData.CurrentSchemaVersion)
            {
                var migratedFrom = saveData.SchemaVersion;

                switch (saveData.SchemaVersion)
                {
                    // Add a case per version as the schema grows, each one stepping the version by
                    // exactly one so a save can migrate across several versions in sequence:
                    //
                    // case 1:
                    //     saveData.SomethingNew = DeriveFromOldFields(saveData);
                    //     saveData.SchemaVersion = 2;
                    //     break;

                    default:
                        // An older version we have no migration for. Whatever deserialized is kept
                        // and the rest takes field defaults - losing a field beats refusing to load
                        // and handing the player an empty save.
                        Debug.LogWarning($"[Save] No migration from schema {saveData.SchemaVersion}; " +
                                         "keeping whatever deserialized and stamping current.");
                        saveData.SchemaVersion = SaveData.CurrentSchemaVersion;
                        break;
                }

                if (saveData.SchemaVersion > migratedFrom)
                {
                    continue;
                }

                // A migration that failed to advance the version would spin here forever.
                Debug.LogError($"[Save] Migration from schema {migratedFrom} did not advance the " +
                               "version. Stamping current to avoid a loop.");
                saveData.SchemaVersion = SaveData.CurrentSchemaVersion;
            }

            return saveData;
        }
    }
}
