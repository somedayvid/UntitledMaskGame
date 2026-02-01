using UnityEngine;

public enum CardType
{
    Attack,
    Defense,
    Power,
    Heal
}
public enum CardEffect
{
    Null,
    DrunkenFist,
    PalmStrike,
    SmallShieldPotion,
    ShieldPotion,
    OrientalMedicineJug, 
    OrientalDaggerRitual,
    OrientalDagger,
    Meditate,
    OrientalTigerBalm,
    GinsengRoot,
    HeavenlyInsight,
    //MandateOfHeaven,
    SunTzusInsight,
    DragonStrike,
    HeavenSplit,
    JadeBarrier,
    //Momentum,
    RockThrow,
    Pills,
    BuddahStrike,
    SubmachineGun,
    
}
//[CreateAssetMenu(fileName = "Card", menuName = "ScriptableObjects/Card", order = 1)]

public class Card
{
    public int ID;
    public string cardName;
    public CardType cardType;
    public CardEffect cardEffect;
    public int chi;
    // define your card properties here
    public int damage;
    public int shield;
    public Card()
    {
        damage = 5;
        chi = 1;
        cardType = CardType.Attack;
    }
    //example I will try something - Ricky
    // can use abstract or virtual. I will use virtual for now
    // see RedMask.cs for example of override (i just made this. change it however you want)
    void ResolveEffect()
    {
        switch (cardEffect)
        {
            case CardEffect.DrunkenFist:
                int rng = Random.Range(0, 2);
                damage = (rng == 0) ? 0 : 12;
                break;
            case (CardEffect.PalmStrike):
                damage = 5;
                break;
            case (CardEffect.SmallShieldPotion):
                chi = 1;
                shield = 10;
                cardType = CardType.Defense;
                break;
            case (CardEffect.ShieldPotion):
                chi = 2;
                shield = 25;
                cardType = CardType.Defense;
                break;
            case (CardEffect.OrientalMedicineJug):
                chi = 3;
                shield = 50;
                cardType = CardType.Defense;
                break;
            case (CardEffect.OrientalDaggerRitual):
                Card temp = new Card();
                temp.cardEffect = CardEffect.OrientalDagger;
                DeckManager.GetInstance().AddCard(temp);
                DeckManager.GetInstance().AddCard(temp);
                DeckManager.GetInstance().AddCard(temp);
                break;
            case (CardEffect.OrientalDagger):
                chi = 0;
                damage = 3;
                break;
            case (CardEffect.Meditate):
                break;
            case (CardEffect.OrientalTigerBalm):
                break;
            case (CardEffect.GinsengRoot):
                break;
            case (CardEffect.HeavenlyInsight):
                DeckManager.GetInstance().Draw();
                DeckManager.GetInstance().Draw();
                DeckManager.GetInstance().Draw();
          
                break;
            //case (CardEffect.MandateOfHeaven):
            //    break;
            case (CardEffect.SunTzusInsight):
                break;
            case (CardEffect.DragonStrike):
                damage = 5;
                break;
            case (CardEffect.HeavenSplit):
                damage = 5;
                break;
            case (CardEffect.JadeBarrier):
                break;
            //case (CardEffect.Momentum):
            //    break;
            case (CardEffect.RockThrow):
                damage = 3;
                chi = 0;
                break;
            case (CardEffect.Pills):
                break;
            case CardEffect.SubmachineGun:
                damage = 10;
                chi = 0;
                break;
            case CardEffect.BuddahStrike:
                break;
        }
    }
        public string GetName()
        {
            switch (cardEffect)
            {
                case CardEffect.Null:
                    return "?";

                case CardEffect.DrunkenFist:
                    return "Drunken Fist";

                case CardEffect.PalmStrike:
                    return "Palm Strike";

                case CardEffect.SmallShieldPotion:
                    return "Minor Shield Potion";

                case CardEffect.ShieldPotion:
                    return "Shield Potion";

                case CardEffect.OrientalMedicineJug:
                    return "Jug of Oriental Medicine";

                case CardEffect.SubmachineGun:
                    return "Submachine Gun";

                case CardEffect.OrientalDaggerRitual:
                    return "Dagger Ritual";

                case CardEffect.OrientalDagger:
                    return "Oriental Dagger";

                case CardEffect.Meditate:
                    return "Meditate";

                case CardEffect.OrientalTigerBalm:
                    return "Tiger Balm";

                case CardEffect.GinsengRoot:
                    return "Ginseng Root";

                case CardEffect.HeavenlyInsight:
                    return "Heavenly Insight";

                //case CardEffect.MandateOfHeaven:
                //    return "Mandate of Heaven";

                case CardEffect.SunTzusInsight:
                    return "Sun Tzu’s Insight";

                case CardEffect.DragonStrike:
                    return "Dragon Strike";

                case CardEffect.HeavenSplit:
                    return "Heaven-Splitting Blow";

                case CardEffect.JadeBarrier:
                    return "Jade Barrier";

                //case CardEffect.Momentum:
                //    return "Momentum";

                case CardEffect.RockThrow:
                    return "Rock Throw";

                case CardEffect.Pills:
                    return "Mystic Pills";
            case CardEffect.BuddahStrike:
                    return "Super Buddah Strike";
                default:    
                    return "-";
            }
        }

    public Sprite GetImage()
    {
        switch (cardEffect)
        {
            case CardEffect.Null:
                return null;

            case CardEffect.DrunkenFist:
                return CardImageList.GetInstance().Img(0);

            case CardEffect.PalmStrike:
                return CardImageList.GetInstance().Img(1);

            case CardEffect.SmallShieldPotion:
                return CardImageList.GetInstance().Img(2);

            case CardEffect.ShieldPotion:
                return CardImageList.GetInstance().Img(3);

            case CardEffect.OrientalMedicineJug:
                return CardImageList.GetInstance().Img(4);

            case CardEffect.SubmachineGun:
                return CardImageList.GetInstance().Img(5);

            case CardEffect.OrientalDaggerRitual:
                return CardImageList.GetInstance().Img(6);

            case CardEffect.OrientalDagger:
                return CardImageList.GetInstance().Img(7);

            case CardEffect.Meditate:
                return CardImageList.GetInstance().Img(8);

            case CardEffect.OrientalTigerBalm:
                return CardImageList.GetInstance().Img(9);

            case CardEffect.GinsengRoot:
                return CardImageList.GetInstance().Img(10);

            case CardEffect.HeavenlyInsight:
                return CardImageList.GetInstance().Img(11);
            case CardEffect.SunTzusInsight:
                return CardImageList.GetInstance().Img(12);

            case CardEffect.DragonStrike:
                return CardImageList.GetInstance().Img(13);

            case CardEffect.HeavenSplit:
                return CardImageList.GetInstance().Img(14);

            case CardEffect.JadeBarrier:
                return CardImageList.GetInstance().Img(15);
            case CardEffect.RockThrow:
                return CardImageList.GetInstance().Img(16);
            case CardEffect.Pills:
                return CardImageList.GetInstance().Img(17);
            case CardEffect.BuddahStrike:
                return CardImageList.GetInstance().Img(18);
            default:
                return null;
        }
    }


    public int GetCost()
    {
        return chi;
    }

    public virtual void UseCard()
    {
        ResolveEffect();
        Debug.Log($"Using card: {GetName()} with ID: {ID}");
    }
}
