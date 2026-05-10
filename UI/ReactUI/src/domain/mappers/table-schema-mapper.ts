import { TableSchemaDtoSchema, type TableSchemaDto } from '../dto/table-schema';
import type { TableSchema } from '../models/table-schema';

const toDomain = TableSchemaDtoSchema.transform(
  (dto: TableSchemaDto): TableSchema => ({
    name: dto.Name,
    columns: dto.Columns.map((col) => ({
      name: col.Name,
      sqlType: col.SqlType,
      maxLength: col.MaxLength,
      isNullable: col.IsNullable,
      isPrimaryKey: col.IsPrimaryKey,
      isIdentity: col.IsIdentity,
      isComputed: col.IsComputed,
      isUnique: col.IsUnique,
      isPlain: col.IsPlain,
      hasDefault: col.HasDefault,
    })),
    connections: dto.Connections.map((fc) => ({
      target: fc.TargetTable,
      mappings: fc.Mappings.map((m) => ({
        source: m.Source,
        target: m.Target,
      })),
    })),
  }),
);

export const TableSchemaMapper = {
  toDomain,
};
