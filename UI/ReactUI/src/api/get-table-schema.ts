import z from 'zod';
import { TableSchemaMapper } from '../domain/mappers/table-schema-mapper';

export async function getTableSchema() {
  const response = await fetch('/api/schema');

  if (!response.ok) {
    throw new Error(
      `Server returned ${response.status}: ${response.statusText} at /api/schema`,
    );
  }

  const text = await response.text();
  if (!text) {
    throw new Error('Server returned an empty response body.');
  }

  const data = JSON.parse(text);

  return z
    .object({
      tables: z.array(TableSchemaMapper.toDomain),
    })
    .parse(data);
}
