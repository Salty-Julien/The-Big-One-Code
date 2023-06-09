using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class SkeletonKing : EnemyController
{
    [Header("Skeleton King")]
    [SerializeField] float attackSpeed;
    [SerializeField] AnimationClip standUpClip;
    [SerializeField] RuntimeAnimatorController burningController;
    [SerializeField] AnimationClip switchPhaseClip;
    [SerializeField] AnimationClip deathClip;
    [SerializeField] float minTimeBetweenAttack;
    [SerializeField] float maxTimeBetweenAttack;
    [SerializeField] DialogueSO skeletonKingDeathSO;
    [SerializeField] Transform canvasDialogue;

    [Header("Switch Lights")]
    [SerializeField] GameObject[] orangeLights;
    [SerializeField] GameObject[] violetLights;
    [SerializeField] ParticleSystem[] lightsPop;

    [Header("Slash")]
    [SerializeField] float timeBetweenSlash;
    [SerializeField] float slashDamage;
    [SerializeField] AnimationClip slashClip;
    [SerializeField] PolygonCollider2D slashCollider;

    [Header("Dash")]
    [SerializeField] float dashSpeed;
    [SerializeField] AnimationClip prepareDashClip;
    [SerializeField] Transform[] dashPositions;
    [SerializeField] PolygonCollider2D dashCollider;
    [SerializeField] float dashDamage;

    [Header("Sword")]
    [SerializeField] Transform swordPosition;
    [SerializeField] float jumpSpeed;
    [SerializeField] float downSpeed;
    [SerializeField] AnimationClip prepareSwordClip;
    [SerializeField] AnimationClip endSwordClip;
    [SerializeField] PolygonCollider2D swordCollider;
    [SerializeField] float swordDamage;
    [SerializeField] GameObject leftGroundRocks;
    [SerializeField] GameObject rightGroudRocks;

    [Header("Ground Kick")]
    [SerializeField] AnimationClip groundKickClip;
    [SerializeField] PolygonCollider2D groundKickCollider;
    [SerializeField] float groundKickDamage;

    [Header("Rocks")]
    [SerializeField] GameObject rockPrefab;
    [SerializeField] BoxCollider2D rocksSpawnArea;

    [Header("Incantation")]
    [SerializeField] Transform incantationPosition;
    [SerializeField] Transform ghostSpawnPositions;
    [SerializeField] GameObject villagerGhostPrefab;
    [SerializeField] float numberOfGhostToSpawn;
    [SerializeField] AnimationClip startIncantationClip;
    [SerializeField] AnimationClip stopIncantationClip;
    [SerializeField] float timeBetweenIncantation;
    [SerializeField] float timeBetweenGhostSpawn;

    [Header("Audio Source")]
    [SerializeField] AudioSource switchingPhaseAS;
    [SerializeField] AudioSource groundKickAS;
    [SerializeField] AudioSource prepareDashAS;
    [SerializeField] AudioSource swordGroundAS;
    [SerializeField] AudioSource slashAS;
    [SerializeField] AudioSource dashAS;

    Animator[] leftAnimators;
    Animator[] rightAnimators;

    float timeSinceLastSlash = Mathf.Infinity;
    float timeSinceLastAttack;
    float timeSinceLastIncantation;
    float timeBetweenAttack;
    float startYPosition;

    bool cantMove;
    bool alreadyAttacking;
    bool exitRoom;
    bool isInDash;
    bool sword;
    bool switchingPhase;

    int attackIndex;
    int maxRocksInARow;
    int currentPhase;

    private void Start()
    {
        Camera.main.GetComponent<Animator>().enabled = true;

        AddEnemyToList(gameObject);
        CantMove(true);
        SetCanAttack(true);
        SetEnableActivity(false);

        startYPosition = transform.position.y;

        StartCoroutine(Cinematic());

        timeBetweenAttack = UnityEngine.Random.Range(minTimeBetweenAttack, maxTimeBetweenAttack);

        leftAnimators = leftGroundRocks.GetComponentsInChildren<Animator>();
        rightAnimators = rightGroudRocks.GetComponentsInChildren<Animator>();

        slashDamage *= GetSimulationsPlaceHolder().GetEnemyDamageMultiplier();
        dashDamage *= GetSimulationsPlaceHolder().GetEnemyDamageMultiplier();
        swordDamage *= GetSimulationsPlaceHolder().GetEnemyDamageMultiplier();
        groundKickDamage *= GetSimulationsPlaceHolder().GetEnemyDamageMultiplier();
    }

    private void Update()
    {
        if (GetEnemyHealth().GetIsDead() && !exitRoom && !sword && currentPhase == 1 && !switchingPhase)
        {
            StopAllCoroutines();

            StartCoroutine(ExitRoom());
        }
        if (cantMove) return;
        if (alreadyAttacking) return;

        if (GetEnemyHealth().GetCurrentHealth() < GetEnemyHealth().GetBaseHealth() * 0.50f && currentPhase == 0)
        {
            currentPhase++;
            StartCoroutine(SwitchPhase());
        }

        timeSinceLastSlash += Time.deltaTime;
        timeSinceLastAttack += Time.deltaTime;
        timeSinceLastIncantation += Time.deltaTime;
    }

    public override void Attack()
    {
        SwitchColliders();

        if (isInDash)
        {
            CheckDashCollider();
        }

        if (cantMove) return;

        if (alreadyAttacking) return;
        if (timeSinceLastAttack <= timeBetweenAttack)
        {
            Slash();
            return;
        }

        timeBetweenAttack = UnityEngine.Random.Range(minTimeBetweenAttack, maxTimeBetweenAttack);
        timeSinceLastAttack = 0f;

        attackIndex = UnityEngine.Random.Range(0, 4);
        
        if (attackIndex == 0 && timeBetweenIncantation < timeSinceLastIncantation)
        {
            StartCoroutine(Incantation());
        }
        else if (attackIndex == 1)
        {
            StartCoroutine(SwordGround());
        }
        else if (attackIndex == 2)
        {
            StartCoroutine(Dash());
        }
        else
        {
            StartCoroutine(GroundKick());
        }
    }

    private IEnumerator SwitchPhase()
    {
        switchingPhase = true;

        SetCanAttack(false);

        minTimeBetweenAttack = 2;
        maxTimeBetweenAttack = 5;
        attackSpeed = 5;

        yield return ReturnToStart();

        GetAnimator().SetBool("walk", false);
        GetAnimator().SetTrigger("switchPhase");

        yield return new WaitForSeconds(0.4f);

        switchingPhaseAS.Play();

        yield return new WaitForSeconds(switchPhaseClip.length - 0.5f);

        GetAnimator().runtimeAnimatorController = burningController;

        yield return SwitchLights();

        yield return new WaitForSeconds(1f);

        SetCanAttack(true);

        GetCurse().BleedCurse(true);

        switchingPhase = false;
    }

    private IEnumerator SwitchLights()
    {
        for (int i = 0; i < orangeLights.Length; i++)
        {
            orangeLights[i].SetActive(false);

            lightsPop[i].Play();

            violetLights[i].SetActive(true);

            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator SwordGround()
    {
        alreadyAttacking = true;
        sword = true;

        GetAnimator().SetBool("walk", false);
        GetAnimator().SetTrigger("prepareSword");

        swordGroundAS.Play();

        yield return new WaitForSeconds(prepareSwordClip.length - 0.4f);

        yield return JumpToSwordPosition();
        yield return DownSword();

        GetAnimator().SetTrigger("endSword");

        yield return GroundRocks();

        sword = false;
        alreadyAttacking = false;
    }

    // Use in prepare sword animation
    public void PrepareDashAS()
    {
        prepareDashAS.Play();
    }

    private IEnumerator GroundRocks()
    {
        int i = 0;

        while (i < leftAnimators.Length)
        {
            leftAnimators[i].SetTrigger("groundRock");
            rightAnimators[i].SetTrigger("groundRock");

            i++;

            if (currentPhase == 0)
            {
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    private IEnumerator JumpToSwordPosition()
    {
        FlipSprite(swordPosition.position.x);

        while (transform.position != swordPosition.position)
        {
            Vector2 targetPosition = new(swordPosition.position.x, swordPosition.position.y);

            transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * jumpSpeed);

            yield return null;
        }
    }

    private IEnumerator DownSword()
    {
        while (transform.position.y != startYPosition)
        {
            Vector2 targetPosition = new(transform.position.x, startYPosition);

            transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * downSpeed);

            yield return null;
        }
    }

    private IEnumerator GroundKick()
    {
        alreadyAttacking = true;
        cantMove = true;

        maxRocksInARow = UnityEngine.Random.Range(0, 3);

        for (int i = 0; i < maxRocksInARow; i++)
        {
            GetAnimator().SetTrigger("groundKick");

            yield return new WaitForSeconds(groundKickClip.length - 0.5f);

            groundKickAS.Play();

            GetCameraShake().Shake(1f, 0.3f, 1.4f);

            StartCoroutine(SpawnRocks());

            if (currentPhase == 0)
            {
                yield return new WaitForSeconds(0.4f);
            }
            else
            {
                yield return new WaitForSeconds(0.15f);
            }
        }

        if (currentPhase == 1)
        {
            yield return new WaitForSeconds(0.25f);
        }

        cantMove = false;
        alreadyAttacking = false;
    }

    private IEnumerator SpawnRocks()
    {
        int numberOfRocks = UnityEngine.Random.Range(10, 21);

        for (int i = 0; i < numberOfRocks; i++)
        {
            Vector2 rockPosition = new(RandomPointInBounds(rocksSpawnArea.bounds), rocksSpawnArea.transform.position.y);

            Instantiate(rockPrefab, rockPosition, Quaternion.identity);

            yield return new WaitForSeconds(0.5f);
        }
    }

    private float RandomPointInBounds(Bounds bounds)
    {
        return UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
    }

    private void CheckDashCollider()
    {
        if (dashCollider.IsTouching(GetPlayerController().GetBodyCollider()))
        {
            float damage = GetEnemyHealth().GetGenerateDamage().GetRandomDamage(dashDamage, false, false);
            bool isCritical = GetEnemyHealth().GetGenerateDamage().GetIsCriticalDamage();

            GetPlayerHealth().TakeDamage(damage, isCritical, false, false, true, true);
        }
    }

    //Use in slash animation
    public void CheckSlashCollider()
    {
        if (slashCollider.IsTouching(GetPlayerController().GetBodyCollider()))
        {
            float damage = GetEnemyHealth().GetGenerateDamage().GetRandomDamage(slashDamage, false, false);
            bool isCritical = GetEnemyHealth().GetGenerateDamage().GetIsCriticalDamage();

            GetPlayerHealth().TakeDamage(damage, isCritical, true, false, true, true);
        }
    }

    //Use in ground kick animation
    public void CheckGroundKickCollider()
    {
        if (groundKickCollider.IsTouching(GetPlayerController().GetBodyCollider()))
        {
            float damage = GetEnemyHealth().GetGenerateDamage().GetRandomDamage(groundKickDamage, false, false);
            bool isCritical = GetEnemyHealth().GetGenerateDamage().GetIsCriticalDamage();

            GetPlayerHealth().TakeDamage(damage, isCritical, true, false, true, true);
        }
    }

    //Use in sword animation
    public void CheckSwordCollider()
    {
        if (swordCollider.IsTouching(GetPlayerController().GetBodyCollider()))
        {
            float damage = GetEnemyHealth().GetGenerateDamage().GetRandomDamage(swordDamage, false, false);
            bool isCritical = GetEnemyHealth().GetGenerateDamage().GetIsCriticalDamage();

            GetPlayerHealth().TakeDamage(damage, isCritical, true, false, true, true);
        }
    }

    private IEnumerator Cinematic()
    {
        DisablePlayerControlls(true);

        yield return new WaitForSeconds(5f);

        yield return GetComponent<LaunchDialogue>().StartDialogue();

        DisablePlayerControlls(true);

        GetAnimator().SetTrigger("standUp");

        yield return new WaitForSeconds(standUpClip.length + 0.5f);

        DisablePlayerControlls(false);

        CantMove(false);
        SetEnableActivity(true);

        Camera.main.GetComponent<Animator>().enabled = false;
    }

    private void DisablePlayerControlls(bool state)
    {
        GetPlayerController().SetIsInActivity(state);
        GetPlayerController().disableControl = state;

        if (state)
        {
            GetPlayerController().StopPlayer(true);
        }
    }

    private IEnumerator Incantation()
    {
        alreadyAttacking = true;

        yield return ReturnToStart();

        GetAnimator().SetTrigger("startIncantation");

        yield return new WaitForSeconds(startIncantationClip.length + 1f);

        yield return GhostIncantation();

        GetAnimator().SetTrigger("stopIncantation");

        yield return new WaitForSeconds(stopIncantationClip.length + 1f);

        timeSinceLastIncantation = 0f;

        alreadyAttacking = false;
    }

    private IEnumerator GhostIncantation()
    {
        int numberOfSpawn = 0;

        while (numberOfSpawn != numberOfGhostToSpawn)
        {
            int spawnIndex = UnityEngine.Random.Range(0, ghostSpawnPositions.childCount);

            SpawnGhost(spawnIndex);

            numberOfSpawn++;

            yield return new WaitForSeconds(timeBetweenGhostSpawn);
        }
    }

    private void SpawnGhost(int spawnIndex)
    {
        VillagerGhost villagerGhost = Instantiate(villagerGhostPrefab, ghostSpawnPositions.GetChild(spawnIndex)).GetComponent<VillagerGhost>();

        villagerGhost.Incanted(false);
    }

    private IEnumerator Dash()
    {
        alreadyAttacking = true;

        if (!GetSpriteRenderer().flipX && Vector3.Distance(transform.position, dashPositions[1].position) < 6f ||
            GetSpriteRenderer().flipX && Vector3.Distance(transform.position, dashPositions[0].position) < 6f || GetDistance() < 4f)
        {
            alreadyAttacking = false;

            yield break;
        }

        GetAnimator().SetBool("walk", false);
        GetAnimator().SetTrigger("prepareDash");

        yield return new WaitForSeconds(prepareDashClip.length);

        GetAnimator().SetTrigger("dash");

        isInDash = true;

        dashAS.Play();

        if (GetSpriteRenderer().flipX)
        {
            yield return StartCoroutine(Dashing(dashPositions[0].position.x));
        }
        else
        {
            yield return StartCoroutine(Dashing(dashPositions[1].position.x));
        }

        GetAnimator().SetTrigger("endDash");

        isInDash = false;
        alreadyAttacking = false;
    }

    private IEnumerator Dashing(float endPositionX)
    {
        while (transform.position.x != endPositionX)
        {
            Vector2 targetPosition = new(endPositionX, startYPosition);

            transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * dashSpeed);

            FlipSprite(endPositionX);

            yield return null;
        }
    }

    private IEnumerator ReturnToStart()
    {
        GetBodyCollider().enabled = false;

        while (transform.position.x != incantationPosition.position.x)
        {
            GetAnimator().SetBool("walk", true);

            Vector2 targetPosition = new(incantationPosition.position.x, startYPosition);

            transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * (attackSpeed +1 ));

            FlipSprite(incantationPosition.position.x);

            yield return null;
        }

        GetAnimator().SetBool("walk", false);

        FlipSprite(incantationPosition.position.x - 1f);

        GetBodyCollider().enabled = true;
    }

    private void Slash()
    {
        FlipSprite(GetTarget().transform.position.x);

        if (GetDistance() < 2.6f && timeBetweenSlash < timeSinceLastSlash)
        {
            GetAnimator().SetBool("walk", false);
            GetAnimator().SetTrigger("attack");

            slashAS.Play();

            timeSinceLastSlash = 0f;
        }
        else if (GetDistance() > 2.6f && slashClip.length < timeSinceLastSlash)
        {
            GetAnimator().SetBool("walk", true);

            Vector2 targetPosition = new(GetTarget().transform.position.x, startYPosition);

            transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * attackSpeed);
        }
        else
        {
            GetAnimator().SetBool("walk", false);
        }
    }

    private void SwitchColliders()
    {
        if (GetEnemyHealth().GetSpriteRenderer().flipX)
        {
            slashCollider.transform.localScale = new Vector3(-1, 1, 1);
            dashCollider.transform.localScale = new Vector3(-1, 1, 1);
            groundKickCollider.transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            slashCollider.transform.localScale = new Vector3(1, 1, 1);
            dashCollider.transform.localScale = new Vector3(1, 1, 1);
            groundKickCollider.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private IEnumerator ExitRoom()
    {
        exitRoom = true;

        GetCurse().BleedCurse(false);

        foreach (var enemy in FindObjectsOfType<EnemyHealth>())
        {
            enemy.TakeDamage(Mathf.Infinity, false, false);
        }

        yield return ReturnToStart();

        GetAnimator().SetTrigger("death");

        yield return new WaitForSeconds(deathClip.length + 1f);

        canvasDialogue.localPosition = new(-4.8f, 0.4f);

        yield return GetComponent<LaunchDialogue>().EndDialogue(skeletonKingDeathSO, true);

        yield return FindObjectOfType<Fader>().FadeOut(2f);

        GetEndGame().DisplayPanel();
        GetSectorTimer().SetGameFinished();
        GetGameManager().AddWinStreak();
    }

    public void CantMove(bool state)
    {
        cantMove = state;
    }

    private void OnDrawGizmos()
    {
        if (!ghostSpawnPositions) return;

        for (int i = 0; i < ghostSpawnPositions.childCount; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ghostSpawnPositions.GetChild(i).position, 0.25f);
        }
    }
}
