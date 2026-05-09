import { Background, BackgroundVariant, Controls, MiniMap, ReactFlow } from '@xyflow/react';
import { useEffect, useState } from 'react';
import { getTableSchema } from './api/get-table-schema';
import styles from './app.module.css';
import { DatabaseSchema } from './components/database-schema';
import { Spinner } from './components/ui/spinner';
import { ReactFlowNodeMapper } from './domain/mappers/react-flow-mapper';
import { DatabaseFlowSchema } from './domain/models/node';

const nodeTypes = {
  databaseSchema: DatabaseSchema,
};

function App() {
  const [schema, setSchema] = useState<DatabaseFlowSchema>(DatabaseFlowSchema.create());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function getData() {
      setLoading(true);

      try {
        const response = await getTableSchema();
        setSchema(ReactFlowNodeMapper.toNode.parse(response.tables));
      } catch (error) {
        setError(error instanceof Error ? error.message : 'An unexpected error occurred');
      }

      setLoading(false);
    }

    getData();
  }, []);

  if (loading)
    return (
      <div className={styles.loading}>
        <Spinner />
      </div>
    );
  if (error) return <div className={styles.error}>Something went wrong: {error}</div>;

  return (
    <div className={styles.container}>
      <ReactFlow
        colorMode="dark"
        defaultNodes={schema.nodes}
        defaultEdges={schema.edges}
        nodeTypes={nodeTypes}
        nodesConnectable={false}
        fitView
      >
        <Background variant={BackgroundVariant.Dots} />
        <Controls />
        <MiniMap />
      </ReactFlow>
    </div>
  );
}

export default App;
