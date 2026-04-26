using System;

public interface IHealth{
float maxHealth { get;}
float CurrentHealth { get;}

float ModifyHealth(float amount);

event Action<float> OnHealthModified;

}
