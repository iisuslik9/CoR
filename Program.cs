
using System;
using System.Collections.Generic;
using System.Linq;

// --- Игровые сущности ---

/// <summary>
/// Представляет игрока с его текущим состоянием.
/// </summary>
public class Player
{
    public string Name { get; }
    public int Health { get; set; }
    
    // Список активных эффектов, которые сработают при атаке на этого игрока.
    public List<IEventHandler> ActiveEffects { get; } = new List<IEventHandler>();

    public Player(string name, int health)
    {
        Name = name;
        Health = health;
    }

    public void TakeDamage(int amount)
    {
        if (amount > 0)
        {
            Health -= amount;
            Console.WriteLine($"	- Игрок {Name} получает {amount} урона.");
        }
    }
    
    public void Heal(int amount)
    {
        if (amount > 0)
        {
            Health += amount;
            Console.WriteLine($"	+ Игрок {Name} восстанавливает {amount} здоровья.");
        }
    }

    public override string ToString()
    {
        return $"[Игрок: {Name}, Здоровье: {Health}]";
    }
}

/// <summary>
/// Представляет игровое событие, передаваемое по цепочке обработчиков.
/// </summary>
public class GameEvent
{
    public Player Attacker { get; }
    public Player Target { get; }
    public int Damage { get; set; }
    public bool IsCancelled { get; set; } = false;

    public GameEvent(Player attacker, Player target, int damage)
    {
        Attacker = attacker;
        Target = target;
        Damage = damage;
    }
}


// --- Паттерн "Цепочка обязанностей" ---

/// <summary>
/// Общий интерфейс для всех обработчиков событий в цепочке.
/// </summary>
public interface IEventHandler
{
    void SetNext(IEventHandler next);
    void Handle(GameEvent gameEvent);
}

/// <summary>
/// Абстрактный базовый класс для обработчиков.
/// Реализует логику передачи события по цепочке и проверку на отмену.
/// </summary>
public abstract class BaseEffectHandler : IEventHandler
{
    protected IEventHandler? _nextHandler;

    public void SetNext(IEventHandler next)
    {
        _nextHandler = next;
    }

    // Основной метод, который вызывается извне. Он управляет потоком.
    public void Handle(GameEvent gameEvent)
    {
        // Если событие уже отменено предыдущим обработчиком, ничего не делаем.
        if (gameEvent.IsCancelled)
        {
            return;
        }

        // Выполняем логику текущего обработчика.
        Process(gameEvent);

        // Если событие не было отменено текущим обработчиком, передаем его дальше по цепочке.
        _nextHandler?.Handle(gameEvent);
    }

    // Абстрактный метод, который должны реализовать конкретные эффекты.
    protected abstract void Process(GameEvent gameEvent);
}

/// <summary>
/// Конкретный обработчик: Эффект "Полная блокировка"
/// </summary>
public class BlockDamageHandler : BaseEffectHandler
{
    public override string ToString() => "Эффект: Блокировка";
    protected override void Process(GameEvent gameEvent)
    {
        Console.WriteLine("-> Сработал эффект 'Блокировка'. Урон полностью поглощен.");
        gameEvent.Damage = 0;
        gameEvent.IsCancelled = true; // Отменяем событие, чтобы другие эффекты не сработали.
    }
}

/// <summary>
/// Конкретный обработчик: Эффект "Отражение"
/// </summary>
public class ReflectDamageHandler : BaseEffectHandler
{
    private readonly double _reflectionRatio;
    public ReflectDamageHandler(double reflectionRatio = 0.5) { _reflectionRatio = reflectionRatio; }
    public override string ToString() => $"Эффект: Отражение ({_reflectionRatio:P0})";

    protected override void Process(GameEvent gameEvent)
    {
        int reflectedDamage = (int)(gameEvent.Damage * _reflectionRatio);
        Console.WriteLine($"-> Сработал эффект 'Отражение'. Атакующий получает {reflectedDamage} урона.");
        gameEvent.Attacker.TakeDamage(reflectedDamage);
    }
}

/// <summary>
/// Конкретный обработчик: Эффект "Вампиризм"
/// </summary>
public class VampirismHandler : BaseEffectHandler
{
    private readonly double _lifestealRatio;
    public VampirismHandler(double lifestealRatio = 0.4) { _lifestealRatio = lifestealRatio; }
    public override string ToString() => $"Эффект: Вампиризм ({_lifestealRatio:P0})";
    
    protected override void Process(GameEvent gameEvent)
    {
        int healedAmount = (int)(gameEvent.Damage * _lifestealRatio);
        Console.WriteLine($"-> Сработал эффект 'Вампиризм'. Атакующий лечится на {healedAmount} HP.");
        gameEvent.Attacker.Heal(healedAmount);
    }
}


// --- Управляющий класс ---

/// <summary>
/// Управляет игровым процессом и нанесением урона.
/// </summary>
public class GameManager
{
    public void DealDamage(Player attacker, Player target, int damage)
    {
        Console.WriteLine($"{attacker.Name} атакует {target.Name}, пытаясь нанести {damage} урона!");
        
        var gameEvent = new GameEvent(attacker, target, damage);

        // Получаем активные эффекты цели.
        var effects = target.ActiveEffects;

        if (!effects.Any())
        {
            Console.WriteLine("У цели нет активных эффектов.");
        }
        else
        {
            Console.WriteLine("Активные эффекты у цели: " + string.Join(", ", effects.Select(e => e.ToString())));
            
            // --- ДИНАМИЧЕСКОЕ ПОСТРОЕНИЕ ЦЕПОЧКИ ---
            // Связываем все эффекты в цепочку: эффект[i] -> эффект[i+1]
            for (int i = 0; i < effects.Count - 1; i++)
            {
                effects[i].SetNext(effects[i + 1]);
            }
            // --- КОНЕЦ ПОСТРОЕНИЯ ЦЕПОЧКИ ---

            // Запускаем обработку события, начиная с первого эффекта в списке.
            effects.First().Handle(gameEvent);
        }
        
        // После прохождения всей цепочки, применяем итоговый урон.
        if (!gameEvent.IsCancelled)
        {
            target.TakeDamage(gameEvent.Damage);
        }
        else
        {
            Console.WriteLine("Урон был полностью отменен.");
        }
        
        Console.WriteLine($"Итог: {attacker} | {target}");
    }
}


// --- Точка входа ---

public class Program
{
    public static void Main(string[] args)
    {
        var gameManager = new GameManager();

        // --- Сценарий 1: Отражение и Вампиризм ---
        var hero = new Player("Рыцарь", 100);
        var villain = new Player("Гоблин", 50);

        // Порядок важен! Сначала сработает отражение, потом вампиризм от исходного урона.
        hero.ActiveEffects.Add(new ReflectDamageHandler(0.5)); // 50% отражение
        hero.ActiveEffects.Add(new VampirismHandler(0.4));    // 40% вампиризм
        
        gameManager.DealDamage(attacker: villain, target: hero, damage: 20);

        // --- Сценарий 2: Порядок изменен + добавлен Блок ---
        Console.WriteLine("============================================");
        var hero2 = new Player("Паладин", 120);
        var villain2 = new Player("Огр", 80);
        
        // Теперь Блок стоит первым. Он отменит событие, и другие эффекты не сработают.
        hero2.ActiveEffects.Add(new BlockDamageHandler());
        hero2.ActiveEffects.Add(new VampirismHandler(1.0)); // Этот эффект не сработает
        
        gameManager.DealDamage(attacker: villain2, target: hero2, damage: 30);
        
        // --- Сценарий 3: Убираем блок, меняем порядок ---
        Console.WriteLine("============================================");
        hero2.ActiveEffects.RemoveAt(0); // Убираем блок
        
        // Теперь первым сработает Вампиризм, а потом Отражение
        hero2.ActiveEffects.Insert(0, new ReflectDamageHandler(0.2));
        
        gameManager.DealDamage(attacker: villain2, target: hero2, damage: 25);
    }
}
