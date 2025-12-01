namespace RootMPI
{
    public enum MessageTag
    {
        // Распределение матрицы от root → workers
        RootToWorker_ColumnCount = 100,
        RootToWorker_ColumnIndices = 101,
        RootToWorker_ColumnData = 102,

        // Сбор опорной строки на шаге k
        PivotColCount = 200,
        PivotColIndices = 201,
        PivotRowValues = 202,

        // Сбор матрицы обратно на root после прямого хода
        WorkerToRoot_ResultColCount = 300,
        WorkerToRoot_ResultColIndices = 301,
        WorkerToRoot_ResultColumnData = 302
    }
}
