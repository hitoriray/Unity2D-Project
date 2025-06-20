namespace Combat.AI
{
    /// <summary>
    /// AI状态接口
    /// 定义了所有具体状态（如待机、巡逻、追逐）必须实现的方法。
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// 当进入该状态时调用
        /// </summary>
        /// <param name="enemy">状态所属的EnemyController实例</param>
        void Enter(EnemyController enemy);

        /// <summary>
        /// 在Update中每帧调用，处理该状态下的核心逻辑
        /// </summary>
        void Update(EnemyController enemy);

        /// <summary>
        /// 当退出该状态时调用
        /// </summary>
        void Exit(EnemyController enemy);
    }
} 