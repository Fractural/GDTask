using System.Collections.Generic;

namespace Fractural.Tasks;

public static partial class GDTaskExtensions
{
    // shorthand of WhenAll

    public static GDTask.Awaiter GetAwaiter(this GDTask[] tasks)
    {
        return GDTask.WhenAll(tasks).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(this IEnumerable<GDTask> tasks)
    {
        return GDTask.WhenAll(tasks).GetAwaiter();
    }

    public static GDTask<T[]>.Awaiter GetAwaiter<T>(this GDTask<T>[] tasks)
    {
        return GDTask.WhenAll(tasks).GetAwaiter();
    }

    public static GDTask<T[]>.Awaiter GetAwaiter<T>(this IEnumerable<GDTask<T>> tasks)
    {
        return GDTask.WhenAll(tasks).GetAwaiter();
    }

    public static GDTask<(T1, T2)>.Awaiter GetAwaiter<T1, T2>(this (GDTask<T1> task1, GDTask<T2> task2) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3)>.Awaiter GetAwaiter<T1, T2, T3>(this (GDTask<T1> task1, GDTask<T2> task2, GDTask<T3> task3) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4)>.Awaiter GetAwaiter<T1, T2, T3, T4>(
        this (GDTask<T1> task1, GDTask<T2> task2, GDTask<T3> task3, GDTask<T4> task4) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5>(
        this (GDTask<T1> task1, GDTask<T2> task2, GDTask<T3> task3, GDTask<T4> task4, GDTask<T5> task5) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6>(
        this (GDTask<T1> task1, GDTask<T2> task2, GDTask<T3> task3, GDTask<T4> task4, GDTask<T5> task5, GDTask<T6> task6) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6, T7>(
        this (GDTask<T1> task1, GDTask<T2> task2, GDTask<T3> task3, GDTask<T4> task4, GDTask<T5> task5, GDTask<T6> task6, GDTask<T7> task7) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6, tasks.Item7).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8>(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8
        ) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6, tasks.Item7, tasks.Item8).GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9
        ) tasks
    )
    {
        return GDTask
            .WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6, tasks.Item7, tasks.Item8, tasks.Item9)
            .GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9,
            GDTask<T10> task10
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10
            )
            .GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9,
            GDTask<T10> task10,
            GDTask<T11> task11
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11
            )
            .GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Awaiter GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9,
            GDTask<T10> task10,
            GDTask<T11> task11,
            GDTask<T12> task12
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12
            )
            .GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Awaiter GetAwaiter<
        T1,
        T2,
        T3,
        T4,
        T5,
        T6,
        T7,
        T8,
        T9,
        T10,
        T11,
        T12,
        T13
    >(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9,
            GDTask<T10> task10,
            GDTask<T11> task11,
            GDTask<T12> task12,
            GDTask<T13> task13
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12,
                tasks.Item13
            )
            .GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Awaiter GetAwaiter<
        T1,
        T2,
        T3,
        T4,
        T5,
        T6,
        T7,
        T8,
        T9,
        T10,
        T11,
        T12,
        T13,
        T14
    >(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9,
            GDTask<T10> task10,
            GDTask<T11> task11,
            GDTask<T12> task12,
            GDTask<T13> task13,
            GDTask<T14> task14
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12,
                tasks.Item13,
                tasks.Item14
            )
            .GetAwaiter();
    }

    public static GDTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Awaiter GetAwaiter<
        T1,
        T2,
        T3,
        T4,
        T5,
        T6,
        T7,
        T8,
        T9,
        T10,
        T11,
        T12,
        T13,
        T14,
        T15
    >(
        this (
            GDTask<T1> task1,
            GDTask<T2> task2,
            GDTask<T3> task3,
            GDTask<T4> task4,
            GDTask<T5> task5,
            GDTask<T6> task6,
            GDTask<T7> task7,
            GDTask<T8> task8,
            GDTask<T9> task9,
            GDTask<T10> task10,
            GDTask<T11> task11,
            GDTask<T12> task12,
            GDTask<T13> task13,
            GDTask<T14> task14,
            GDTask<T15> task15
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12,
                tasks.Item13,
                tasks.Item14,
                tasks.Item15
            )
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(this (GDTask task1, GDTask task2) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(this (GDTask task1, GDTask task2, GDTask task3) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(this (GDTask task1, GDTask task2, GDTask task3, GDTask task4) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(this (GDTask task1, GDTask task2, GDTask task3, GDTask task4, GDTask task5) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(this (GDTask task1, GDTask task2, GDTask task3, GDTask task4, GDTask task5, GDTask task6) tasks)
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (GDTask task1, GDTask task2, GDTask task3, GDTask task4, GDTask task5, GDTask task6, GDTask task7) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6, tasks.Item7).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (GDTask task1, GDTask task2, GDTask task3, GDTask task4, GDTask task5, GDTask task6, GDTask task7, GDTask task8) tasks
    )
    {
        return GDTask.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6, tasks.Item7, tasks.Item8).GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (GDTask task1, GDTask task2, GDTask task3, GDTask task4, GDTask task5, GDTask task6, GDTask task7, GDTask task8, GDTask task9) tasks
    )
    {
        return GDTask
            .WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6, tasks.Item7, tasks.Item8, tasks.Item9)
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (
            GDTask task1,
            GDTask task2,
            GDTask task3,
            GDTask task4,
            GDTask task5,
            GDTask task6,
            GDTask task7,
            GDTask task8,
            GDTask task9,
            GDTask task10
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10
            )
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (
            GDTask task1,
            GDTask task2,
            GDTask task3,
            GDTask task4,
            GDTask task5,
            GDTask task6,
            GDTask task7,
            GDTask task8,
            GDTask task9,
            GDTask task10,
            GDTask task11
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11
            )
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (
            GDTask task1,
            GDTask task2,
            GDTask task3,
            GDTask task4,
            GDTask task5,
            GDTask task6,
            GDTask task7,
            GDTask task8,
            GDTask task9,
            GDTask task10,
            GDTask task11,
            GDTask task12
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12
            )
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (
            GDTask task1,
            GDTask task2,
            GDTask task3,
            GDTask task4,
            GDTask task5,
            GDTask task6,
            GDTask task7,
            GDTask task8,
            GDTask task9,
            GDTask task10,
            GDTask task11,
            GDTask task12,
            GDTask task13
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12,
                tasks.Item13
            )
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (
            GDTask task1,
            GDTask task2,
            GDTask task3,
            GDTask task4,
            GDTask task5,
            GDTask task6,
            GDTask task7,
            GDTask task8,
            GDTask task9,
            GDTask task10,
            GDTask task11,
            GDTask task12,
            GDTask task13,
            GDTask task14
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12,
                tasks.Item13,
                tasks.Item14
            )
            .GetAwaiter();
    }

    public static GDTask.Awaiter GetAwaiter(
        this (
            GDTask task1,
            GDTask task2,
            GDTask task3,
            GDTask task4,
            GDTask task5,
            GDTask task6,
            GDTask task7,
            GDTask task8,
            GDTask task9,
            GDTask task10,
            GDTask task11,
            GDTask task12,
            GDTask task13,
            GDTask task14,
            GDTask task15
        ) tasks
    )
    {
        return GDTask
            .WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7,
                tasks.Item8,
                tasks.Item9,
                tasks.Item10,
                tasks.Item11,
                tasks.Item12,
                tasks.Item13,
                tasks.Item14,
                tasks.Item15
            )
            .GetAwaiter();
    }
}
