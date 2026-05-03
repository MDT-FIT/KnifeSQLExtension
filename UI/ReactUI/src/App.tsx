import { useEffect, useState } from 'react';
import ReactFlow, { Background, BackgroundVariant } from 'reactflow';
import { getTableSchema } from './api/get-table-schema';
import './app.css';
import { DatabaseSchema } from './components/database-schema';
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

  if (loading) return <div>Connecting to C# Server...</div>;
  if (error) return <div style={{ color: 'red' }}>Error: {error}</div>;

  return (
    <div className="App">
      <ReactFlow
        defaultNodes={schema.nodes}
        defaultEdges={schema.edges}
        nodeTypes={nodeTypes}
        fitView
      >
        <Background variant={BackgroundVariant.Dots} />
      </ReactFlow>
    </div>
  );
}

export default App;
